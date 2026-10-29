"""yaml 자세 로드와 관절 보간."""
import yaml


class PoseTable:
    def __init__(self, path):
        with open(path, 'r') as f:
            data = yaml.safe_load(f)
        self.joint_names = list(data['joint_names'])
        self.poses = {k: [float(x) for x in v] for k, v in data['poses'].items()}
        self.sequence = list(data['sequence'])
        for name in self.sequence:
            if name not in self.poses:
                raise ValueError(f'sequence에 정의되지 않은 자세: {name}')
        for name, pose in self.poses.items():
            if len(pose) != len(self.joint_names):
                raise ValueError(f'자세 {name}의 관절 수가 맞지 않음')

    def interpolate(self, start, goal, t):
        """t(0~1)에 해당하는 관절 보간값."""
        t = min(max(t, 0.0), 1.0)
        return [s + (g - s) * t for s, g in zip(start, goal)]
