pipeline {
    agent any

    stages {
        stage('Deploy') {
            steps {
                dir('/opt/apps/Musicly') {
                    sh 'git reset --hard'
                    sh 'git clean -fd'
                    sh 'git pull origin main'
                    sh 'docker compose down'
                    sh 'docker compose up -d --build'
                }
            }
        }
    }
}