# GlobalX Document Service Test App

## Background

This test app is a .NET 4.6.1 WinForms application designed to showcase a number of the integration possibilities with our RESTful document service API using the Resource Owner Password Grant flow for authentication. The application was built in VS2017.

This application utilises some dependencies that can be restored by NuGet:

* RestSharp - used to simplify the REST request used to authenticate with GlobalX
* HoneyBear.HalClient - used to simplify the interaction with the Hypermedia links provided by the Document Service
* Newtonsoft.Json - used to handle JSON (de)serialisation
* Costura - used to roll all dependencies into the final assembly on build for ease of distribution

## Using the App

Firstly, you'll need access to GlobalX's APIs in our test environment, please contact GlobalX Integration Support if you don't currently have a staging account with OAuth2 GlobalX client credentials.

Once you have your credentials ready to go, you can either pre-set the credentials in the App.Config or set them in the provided text fields within the application.

.\DocumentServiceTester\App.config:
```xml
<appSettings>
	<add key="ClientId" value="" />
	<add key="ClientSecret" value="" />
	<add key="Username" value="" />
	<add key="Password" value="" />
</appSettings>
```

Afterwards, follow the directions in the WinForms application to query documents from GlobalX.

**NOTE:** You will need to have documents generated against your staging account to receive them. Log into the [GlobalX staging portal](https://staging-tmg.globalx.com.au) and perform some GlobalX searches.