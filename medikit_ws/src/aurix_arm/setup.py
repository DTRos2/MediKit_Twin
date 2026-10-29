from setuptools import find_packages, setup

package_name = 'aurix_arm'

setup(
    name=package_name,
    version='0.1.0',
    packages=find_packages(exclude=['test']),
    data_files=[
        ('share/ament_index/resource_index/packages', ['resource/' + package_name]),
        ('share/' + package_name, ['package.xml']),
        ('share/' + package_name + '/config', ['config/arm_a_poses.yaml']),
    ],
    install_requires=['setuptools'],
    zip_safe=True,
    maintainer='coc',
    maintainer_email='ajtwlscldjs@gmail.com',
    description='Robot arm A demo commander',
    license='Apache-2.0',
    entry_points={
        'console_scripts': [
            'arm_a_commander = aurix_arm.arm_a_commander:main',
        ],
    },
)
