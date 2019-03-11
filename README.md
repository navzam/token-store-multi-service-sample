# Token Vault Multi-Service Sample
Sample web app that uses Token Vault to manage access tokens to multiple external services. Users must sign in to the app using a work or school account. Then they can authorize the app to access their files from O365 and/or Dropbox.

## Running the sample

### Register an AAD v1 app
First you need to register an AAD v1 app, which will represent the web app's identity in the AAD world. The web app will use this AAD app to both authenticate the user to the web app and get authorized access to the user's O365 files.

> NOTE: This must be an AAD v1 app, not AAD v2 app. You will need to add two redirect URIs with different domains, which is not allowed in AAD v2 apps.

1. Go to the [AAD app registration page](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps) and click "New application registration"
    - Name: choose any name
    - Application type: Web app / API
    - Sign-on URL: choose any URL (you can change this later)
1. On the next page, note down the `Application ID`. You will need this later
1. In `Settings -> Keys`, create a new password and note it down. You will need this later
1. In `Settings -> Required permissions`, add the following permission for the Microsoft Graph API: "Read user files"

### Register a Dropbox app
Similarly you need to regsiter a Dropbox app. The web app will use this Dropbox app to get authorized access to the user's Dropbox files.
1. Go to the [Dropbox developer site](https://www.dropbox.com/developers/apps) and click "Create app"
    - API: "Dropbox API"
    - Type of access: "Full Dropbox"
    - Name: choose any name
1. On the next page, note down the app key and app secret. You will need these later
1. Leave the "Redirect URIs" field blank. You will fill this in later
1. Set "Allow implicit grant" to "Disallow". Token Vault will use the authorization grant flow, so you can disable implicit flow to be safe

### Deploy the solution to Azure

[![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

This repository includes an ARM template that describes the necessary resources. You can easily deploy them to your Azure subscription using the button above. It will create 3 Azure resources:
- App Service: Hosts the sample web app
- App Service Plan: Defines the compute resources and pricing for the App Service
- Token Vault: Stores and manages OAuth access tokens

The template includes some paramters that you will have to fill in:

Parameter          | Description
------------------ | ------------------------------------------------------------------
`tokenVaultName`   | Name of the Token Vault resource. Used in the Token Vault's URL
`dropboxAppId`     | App key assigned when you registered the Dropbox app
`dropboxAppSecret` | App secret assigned when you registered the Dropbox app
`webAppPlanName`   | Name for the App Service Plan resource
`webAppName`       | Name for the App Service resource. Used in the web app's URL
`aadClientId`      | Client ID assigned when you registered the AAD app
`aadClientSecret`  | Client secret assigned when you registered the AAD app


### Set OAuth redirect URLs
One of the outputs of the deployment will be the Token Vault's redirect URL (`tokenVaultRedirectUri`). You need to add this redirect URL to your AAD and Dropbox app registrations.

1. Go back to the Dropbox app and add the redirect URL in the "Redirect URIs" field
1. Go back to the AAD app and add the redirect URL in the "Reply URLs" section. Also add a reply URL for your web app at `/signin-oidc` (for example, `https://mywebapp.azurewebsites.net/signin-oidc`). You can delete any other reply URLs that were added automatically

### Use the web app
Navigate to the App Service resource and click on the URL to open the application.