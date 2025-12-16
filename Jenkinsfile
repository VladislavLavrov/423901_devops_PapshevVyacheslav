pipeline {
    agent any

    stages {
        stage('Hello') {
            steps {
                echo 'Hello jenkins'
            }
        }
        stage('Build Docker Image') {
            steps {
                dir('App_home') {
                    sh 'ls -l'
                    sh 'docker compose build'
                }
            }
            
        }
		stage('Start Docker Container') {
            steps {
                dir('App_home') {
                    sh 'docker compose up -d'
                }
            }
        }
    }
}