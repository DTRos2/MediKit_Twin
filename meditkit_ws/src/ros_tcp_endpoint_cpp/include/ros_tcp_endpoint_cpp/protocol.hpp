// Unity ROS-TCP-Connector 와의 프로토콜 (ROS-TCP-Endpoint v0.7.0 호환)
//
// 프레임: u32 len(dest) | dest | u32 len(payload) | payload   (little-endian)
//   - 일반 메시지: dest = 토픽 이름, payload = CDR 직렬화 바이트
//   - 시스템 명령: dest = "__xxx",   payload = JSON 문자열
#pragma once

#include <sys/socket.h>
#include <sys/types.h>

#include <cctype>
#include <cerrno>
#include <cstdint>
#include <map>
#include <stdexcept>
#include <string>
#include <vector>

namespace ros_tcp_endpoint_cpp
{

using Bytes = std::vector<uint8_t>;

inline void put_u32(Bytes & out, uint32_t v)
{
  for (int i = 0; i < 4; ++i) {
    out.push_back(static_cast<uint8_t>((v >> (8 * i)) & 0xff));
  }
}

inline uint32_t get_u32(const uint8_t * p)
{
  return static_cast<uint32_t>(p[0]) | (static_cast<uint32_t>(p[1]) << 8) |
         (static_cast<uint32_t>(p[2]) << 16) | (static_cast<uint32_t>(p[3]) << 24);
}

inline Bytes make_frame(const std::string & dest, const uint8_t * data, size_t size)
{
  Bytes out;
  out.reserve(8 + dest.size() + size);
  put_u32(out, static_cast<uint32_t>(dest.size()));
  out.insert(out.end(), dest.begin(), dest.end());
  put_u32(out, static_cast<uint32_t>(size));
  out.insert(out.end(), data, data + size);
  return out;
}

inline Bytes make_command(const std::string & cmd, const std::string & json)
{
  return make_frame(cmd, reinterpret_cast<const uint8_t *>(json.data()), json.size());
}

inline bool send_all(int fd, const uint8_t * data, size_t size)
{
  size_t sent = 0;
  while (sent < size) {
    ssize_t n = ::send(fd, data + sent, size - sent, MSG_NOSIGNAL);
    if (n < 0 && errno == EINTR) {continue;}
    if (n <= 0) {return false;}
    sent += static_cast<size_t>(n);
  }
  return true;
}

inline bool recv_all(int fd, uint8_t * data, size_t size)
{
  size_t got = 0;
  while (got < size) {
    ssize_t n = ::recv(fd, data + got, size - got, 0);
    if (n < 0 && errno == EINTR) {continue;}
    if (n <= 0) {return false;}
    got += static_cast<size_t>(n);
  }
  return true;
}

// 수신 프레임 하나를 읽는다. 연결이 끊기면 false.
inline bool read_frame(int fd, std::string & dest, Bytes & payload)
{
  uint8_t len_buf[4];
  if (!recv_all(fd, len_buf, 4)) {return false;}
  uint32_t dest_len = get_u32(len_buf);
  dest.assign(dest_len, '\0');
  if (dest_len > 0 && !recv_all(fd, reinterpret_cast<uint8_t *>(&dest[0]), dest_len)) {
    return false;
  }
  while (!dest.empty() && dest.back() == '\0') {dest.pop_back();}

  if (!recv_all(fd, len_buf, 4)) {return false;}
  uint32_t size = get_u32(len_buf);
  payload.assign(size, 0);
  return size == 0 || recv_all(fd, payload.data(), size);
}

inline std::string json_escape(const std::string & s)
{
  std::string out;
  for (char c : s) {
    switch (c) {
      case '"': out += "\\\""; break;
      case '\\': out += "\\\\"; break;
      case '\n': out += "\\n"; break;
      case '\r': out += "\\r"; break;
      case '\t': out += "\\t"; break;
      default: out += c;
    }
  }
  return out;
}

// 평평한 JSON 객체({"k": "v", "n": 10, "b": true})만 파싱한다. 값은 문자열로 돌려준다.
inline std::map<std::string, std::string> parse_flat_json(const std::string & text)
{
  std::map<std::string, std::string> out;
  size_t i = 0;
  auto skip_ws = [&]() {
      while (i < text.size() && (isspace(static_cast<unsigned char>(text[i])) || text[i] == '\0')) {++i;}
    };
  auto read_string = [&]() {
      std::string s;
      ++i;  // opening quote
      while (i < text.size() && text[i] != '"') {
        if (text[i] == '\\' && i + 1 < text.size()) {
          ++i;
          switch (text[i]) {
            case 'n': s += '\n'; break;
            case 't': s += '\t'; break;
            case 'r': s += '\r'; break;
            default: s += text[i];
          }
        } else {
          s += text[i];
        }
        ++i;
      }
      ++i;  // closing quote
      return s;
    };

  skip_ws();
  if (i >= text.size() || text[i] != '{') {throw std::runtime_error("JSON object expected");}
  ++i;
  while (true) {
    skip_ws();
    if (i >= text.size()) {throw std::runtime_error("unterminated JSON");}
    if (text[i] == '}') {break;}
    if (text[i] == ',') {++i; continue;}
    if (text[i] != '"') {throw std::runtime_error("JSON key expected");}
    std::string key = read_string();
    skip_ws();
    if (i >= text.size() || text[i] != ':') {throw std::runtime_error("':' expected");}
    ++i;
    skip_ws();
    std::string value;
    if (i < text.size() && text[i] == '"') {
      value = read_string();
    } else {
      while (i < text.size() && text[i] != ',' && text[i] != '}' &&
        !isspace(static_cast<unsigned char>(text[i])))
      {
        value += text[i++];
      }
    }
    out[key] = value;
  }
  return out;
}

// Unity 쪽 이름("pkg/Type")과 ROS2 타입 이름("pkg/msg/Type") 변환
inline std::string to_ros_type(const std::string & unity_name, const std::string & kind = "msg")
{
  size_t first = unity_name.find('/');
  if (first == std::string::npos) {return unity_name;}
  if (unity_name.find('/', first + 1) != std::string::npos) {return unity_name;}
  return unity_name.substr(0, first) + "/" + kind + "/" + unity_name.substr(first + 1);
}

inline std::string to_unity_type(const std::string & ros_name)
{
  std::string out = ros_name;
  for (const char * kind : {"/msg/", "/srv/"}) {
    size_t pos = out.find(kind);
    if (pos != std::string::npos) {out.replace(pos, 5, "/");}
  }
  return out;
}

}  // namespace ros_tcp_endpoint_cpp
