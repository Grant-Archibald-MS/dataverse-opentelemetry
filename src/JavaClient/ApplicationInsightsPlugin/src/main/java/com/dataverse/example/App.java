package com.dataverse.example;

import com.google.gson.Gson;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.google.gson.JsonSyntaxException;

import java.io.*;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.file.Files;
import java.nio.file.Paths;

/**
 * This Java application replicates the functionality of a PowerShell script example.
 * It reads configuration from a JSON file, obtains an access token using Azure CLI,
 * and makes HTTP POST requests to specified API endpoints with JSON bodies.
 * 
 * Assumptions:
 * - The configuration file (config.json) is present in the same directory as the application.
 * - The Azure CLI is installed and configured on the system.
 * - The environment URL, custom API name, and entity name are provided in the config.json file.
 * 
 * Dependencies:
 * - Gson library for JSON parsing.
 */
public class App 
{
    // Gson instance for JSON parsing
    private static final Gson gson = new Gson();

    public static void main( String[] args ) throws IOException 
    {
        // Read the configuration file (config.json)
        String configContent = new String(Files.readAllBytes(Paths.get("config.json")));
        JsonObject config = gson.fromJson(configContent, JsonObject.class);

        // Extract values from the configuration file
        String environmentUrl = config.get("environmentUrl").getAsString();
        String customApiName = config.get("customApiName").getAsString();
        String entityName = config.get("entityName").getAsString();

        // Obtain an access token using Azure CLI
        String token = getAccessToken(environmentUrl);
        
        String traceParent = null;

        // If customApiName is provided, make HTTP POST requests to the custom API
        if (customApiName != null && !customApiName.isEmpty() && token != null) {
            System.out.println("Custom API Name: " + customApiName);
            traceParent = postData(environmentUrl + "api/data/v9.0/" + customApiName, token, "Sample", "1", "Information", "Some data");

            System.out.println("TraceParent: " + traceParent);
            String childTraceParent = postData(environmentUrl + "api/data/v9.0/" + customApiName + "?tag=" + traceParent, token, "Sample", "2", "Information", "Some more data");

            System.out.println("TraceParent (Child - Via Tag): " + childTraceParent);
            String grandChildTraceParent = postData(environmentUrl + "api/data/v9.0/" + customApiName, token, "Sample", "3", "Information", "Some further data", childTraceParent);

            System.out.println("TraceParent (Grandchild via Custom API Message): " + grandChildTraceParent);
        }

        // If entityName is provided, make HTTP POST requests to create the entity
        if (entityName != null && !entityName.isEmpty() && traceParent != null && token != null) {
            System.out.println("Entity Name: " + entityName);
            String entityUrl = environmentUrl + "api/data/v9.0/" + entityName + "?tag=" + traceParent;
            JsonObject entityBody = new JsonObject();
            entityBody.addProperty("name", "Test " + entityName);
            entityBody.addProperty("description", "Sample data");

            String response = postData(entityUrl, token, entityBody);
            System.out.println(response);
        }
    }

    /**
     * Obtains an access token using Azure CLI.
     * @param environmentUrl The environment URL for the Azure resource.
     * @return The access token as a string.
     * @throws IOException If an I/O error occurs.
     */
    private static String getAccessToken(String environmentUrl) throws IOException {
        String azExecutable = findAzExecutable();
        if (azExecutable == null) {
            throw new IOException("Azure CLI executable not found");
        }

        File file = new File(azExecutable);

        // Use the found az executable to obtain the access token
        ProcessBuilder processBuilder = new ProcessBuilder("pwsh", "-Command", file.getName() + " account get-access-token --resource=" + environmentUrl + " --query accessToken --output tsv");
        processBuilder.directory(new File(file.getParent()));
        Process process = processBuilder.start();
        return new String(process.getInputStream().readAllBytes()).trim();
    }

    /**
     * Finds the Azure CLI executable based on the operating system.
     * @return The path to the Azure CLI executable.
     */
    private static String findAzExecutable() {
        String os = System.getProperty("os.name").toLowerCase();
        String azExecutable = null;

        if (os.contains("win")) {
            // Windows: Use pwsh to find the location of az
            try {
                ProcessBuilder processBuilder = new ProcessBuilder("pwsh.exe", "-Command", "Get-Command az | Select-Object -ExpandProperty Source");
                Process process = processBuilder.start();
                String output = new String(process.getInputStream().readAllBytes()).trim();
                if (!output.isEmpty()) {
                    return output;
                }
            } catch (IOException e) {
                e.printStackTrace();
            }
        } else if (os.contains("nix") || os.contains("nux") || os.contains("mac")) {
            // Linux or macOS
            String azPath = System.getenv("PATH");
            String[] paths = azPath.split(":");
            for (String path : paths) {
                if (Files.exists(Paths.get(path, "az"))) {
                    azExecutable = Paths.get(path, "az").toString();
                    break;
                }
            }
        }

        return azExecutable;
    }

    /**
     * Makes an HTTP POST request to the specified URL with the provided data.
     * @param url The URL to send the request to.
     * @param token The access token for authorization.
     * @param source The source of the data.
     * @param stage The stage of the data.
     * @param level The level of the data.
     * @param message The message to include in the data.
     * @return The response from the server as a string.
     * @throws IOException If an I/O error occurs.
     */
    private static String postData(String url, String token, String source, String stage, String level, String message) throws IOException {
        return postData(url, token, source, stage, level, message, null);
    }

    /**
     * Makes an HTTP POST request to the specified URL with the provided data and trace parent.
     * @param url The URL to send the request to.
     * @param token The access token for authorization.
     * @param source The source of the data.
     * @param stage The stage of the data.
     * @param level The level of the data.
     * @param message The message to include in the data.
     * @param traceParent The trace parent to include in the data (optional).
     * @return The response from the server as a string.
     * @throws IOException If an I/O error occurs.
     */
    private static String postData(String url, String token, String source, String stage, String level, String message, String traceParent) throws IOException {
        JsonObject body = new JsonObject();
        body.addProperty("Source", source);
        body.addProperty("Stage", stage);
        body.addProperty("Level", level);
        body.addProperty("Message", message);
        if (traceParent != null) {
            body.addProperty("TraceParent", traceParent);
        }
        return postData(url, token, body);
    }

    /**
     * Makes an HTTP POST request to the specified URL with the provided JSON body.
     * @param url The URL to send the request to.
     * @param token The access token for authorization.
     * @param body The JSON body to include in the request.
     * @return The response from the server as a string.
     * @throws IOException If an I/O error occurs.
     */
    private static String postData(String url, String token, JsonObject body) throws IOException {
        URL urlObj = new URL(url);
        HttpURLConnection connection = (HttpURLConnection) urlObj.openConnection();
        connection.setRequestMethod("POST");
        connection.setRequestProperty("Authorization", "Bearer " + token);
        connection.setRequestProperty("Content-Type", "application/json");
        connection.setDoOutput(true);

        try (OutputStream os = connection.getOutputStream()) {
            byte[] input = body.toString().getBytes("utf-8");
            os.write(input, 0, input.length);
        }

        try (BufferedReader br = new BufferedReader(new InputStreamReader(connection.getInputStream(), "utf-8"))) {
            StringBuilder response = new StringBuilder();
            String responseLine;
            while ((responseLine = br.readLine()) != null) {
                response.append(responseLine.trim());
            }
            String responseBody = response.toString();

            if (responseBody.startsWith("{")) 
            {
                // Check if the response is JSON and parse it
                try {
                    JsonObject jsonResponse = JsonParser.parseString(responseBody).getAsJsonObject();
                    if (jsonResponse.has("TraceParent")) {
                        return jsonResponse.get("TraceParent").getAsString();
                    }
                } catch (JsonSyntaxException e) {
                    // Not a JSON response
                }
            }

            return responseBody;
        } finally {
            connection.disconnect();
        }
    }
}