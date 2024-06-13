import os
import sys
import subprocess

sonar_token = os.environ.get('SONAR_TOKEN', '')
if len(sonar_token) < 2:
	print('Please set the environment variable SONAR_TOKEN to your scanner\'s auth token!')
	sys.exit(1)

sonar_server = 'https://sonarqube.prod.resource.jwac.mil'
sonar_project_name = 'CertMama'

subprocess.run([
	'dotnet', 'sonarscanner', 'begin', f'/k:{sonar_project_name}', f'/d:sonar.token={sonar_token}', f'/d:sonar.host.url={sonar_server}'
], check=True)

subprocess.run([
	'dotnet', 'build', 'cert-mama.csproj', '--no-incremental'
], check=True)

subprocess.run([
	'dotnet', 'sonarscanner', 'end', f'/d:sonar.token={sonar_token}',
], check=True)



