pipeline {
    agent any

    stages {

        stage('Docker Compose Build') {
            steps {
                sh 'docker-compose down'
                sh 'docker-compose up -d --build'
            }
        }

    }
}
}

