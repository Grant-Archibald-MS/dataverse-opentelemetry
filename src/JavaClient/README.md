# Overview

## Step 1: Set Up Your Java Project in VS Code

1. Install Java Development Kit (JDK): Ensure you have JDK [installed](https://openjdk.org/install/). For example on windows

```pwsh
winget search Microsoft.OpenJDK
winget install Microsoft.OpenJDK
```

2. Ensure that you have VS Code [installed](https://code.visualstudio.com/download)

3. Install Java Extensions: Open VS Code and install the Java Extension Pack from the Extensions view (Ctrl+Shift+X).

4. Ensure you have maven [installed](https://maven.apache.org/download.cgi) with JAVA_HOME set as environment

> NOTE: This project was created using
> mvn archetype:generate  -DarchetypeArtifactId=maven-archetype-quickstart

5. Change to example plugin

```pwsh
cd src\JavaClient\ApplicationInsightsPlugin
```

6. Compile the code

```pwsh
mvn compile
```

7. Create config.json file. For example 

```json
{
    "environmentUrl": "https://contoso.crm.dynamics.com/",
    "entityName": "accounts",
    "customApiName": "demo_Telemetry"
}
```

8. Run the compiled Java application

```pwsh
mvn exec:java
```
