from setuptools import find_packages, setup

package_name = 'aurix_monitor'

setup(
    name=package_name,
    version='0.1.0',
    packages=find_packages(exclude=['test']),
    data_files=[
        ('share/ament_index/resource_index/packages', ['resource/' + package_name]),
        ('share/' + package_name, ['package.xml']),
    ],
    install_requires=['setuptools'],
    zip_safe=True,
    maintainer='coc',
    maintainer_email='ajtwlscldjs@gmail.com',
    description='Cell process monitor',
    license='Apache-2.0',
    entry_points={
        'console_scripts': [
            'cell_monitor = aurix_monitor.cell_monitor:main',
        ],
    },
)
