String buildVersion

pipeline {
	agent none
	
	environment {
		DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
		DOTNET_CLI_TELEMETRY_OPTOUT = "1"
	}

	options { skipDefaultCheckout() }
	
	stages {
		stage("Build") {
			agent any
			steps {
				deleteDir()
				checkout scm
				buildTarget "Export_Build_Version", "-BuildVersionFilePath \"${env.WORKSPACE}/version.txt\""

				script {
					buildVersion = readFile "${env.WORKSPACE}/version.txt"
					currentBuild.displayName = buildVersion
				}

				buildTarget "Compile"

				stash name: "solution", useDefaultExcludes: false
			}
		}

		stage("Tests") {
			agent any
			steps {
				deleteDir()
				unstash "solution"

				buildTarget "Test", "-NoDeps"
				buildTarget "Verify_Pacts", "-NoDeps"
			}
		}

		stage("Package & Upload") {
			agent any
			steps {
				deleteDir()
				unstash "solution"

			    buildTarget "Package", "-NoDeps"
				buildTarget "Upload", "-NoDeps"
			}
		}

		stage("CI Deployment") {
			agent any
			steps { deploy "wgtdev", "ci", buildVersion, true }
		}

		stage("Deploy to UAT?") {
			agent none
			steps {
				script { env.SkipUat = promptToSkipDeploy "UAT" }
			}
		}

		stage("UAT Deployment") {
			agent any
			when { environment name: "SkipUat", value: "false" }
			steps { deploy "wgtdev", "uat", buildVersion }
		}

		stage("Deploy to SVT?") {
			agent none
			steps {
				script { env.SkipSvt = promptToSkipDeploy "SVT" }
			}
		}

		stage("SVT Deployment") {
			agent any
			when { environment name: "SkipSvt", value: "false" }
			steps { deploy "wgtdev", "svt", buildVersion }
		}

		stage("Deploy to Production?") {
			agent none
			when { branch "master" }
			steps {
				script { env.SkipProduction = promptToSkipDeploy "Production" }
			}
		}

		stage("Production Deployment") {
		    agent any
			when { environment name: "SkipProduction", value: "false" }
			steps { deploy "wgtprod", "prod", buildVersion }
		}
	}

	post {
		failure {
			emailext(
				body: '${JELLY_SCRIPT,template="html-with-health-and-console"}',
				mimeType: "text/html",
				recipientProviders: [[$class: "RequesterRecipientProvider"], [$class: "CulpritsRecipientProvider"]],
				subject: "Build failure: ${env.JOB_NAME} [${env.BUILD_DISPLAY_NAME}]")
		}
	}
}

void buildTarget(String targetName, String parameters = "") {
	ansiColor("xterm") {
		sh "dotnet run -p \"${env.WORKSPACE}/build\" -Target ${targetName} ${parameters}"
	}
}

void cancelOlderPausedJobs() {
	def job = Jenkins.instance.getItemByFullName(env.JOB_NAME)
	def currentBuildNumber = env.BUILD_NUMBER.toInteger()

	for (build in job.builds) {
		if (build.number < currentBuildNumber && 
				build.getAction(org.jenkinsci.plugins.workflow.support.steps.input.InputAction.class)?.displayName == "Paused for Input") {
			echo "Terminating " + build
			build.doStop()
		}
	}
}

void deploy(String awsAccountName, String environment, String versionToDeploy, Boolean cancelJobs = false) {
	deleteDir()
	unstash "solution"

	lock("Bet API ${environment}") {
		if (cancelJobs) {
			cancelOlderPausedJobs()
		}

		buildTarget "Deploy", "-Account ${awsAccountName} -Environment ${environment} -VersionToDeploy ${versionToDeploy}"
	}
}

Boolean promptToSkipDeploy(String environment) {
	timeout(time: 5, unit: 'DAYS') {
		return input(message: "Deploy to ${environment}?", parameters: [booleanParam(name: "Skip?")])
	}
}