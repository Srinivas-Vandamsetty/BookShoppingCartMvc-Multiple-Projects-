const B = require('C:/Users/Srinivas Vandamsetty/.codex/skills/vfi-word-docs/scripts/brand.js');

const doc = B.createDocument({
  title: 'Book Shopping Cart Azure App Service Guide',
  heading: 'Azure Services and Deployment Guide',
  children: () => [
    B.h1('Purpose'),
    B.p('This guide explains the Azure services used to host the Book Shopping Cart application and the deployment sequence to follow when an Azure subscription is available.'),

    B.h1('Azure Services'),
    B.table(['Azure Service', 'Purpose in Book Shopping Cart'], [
      ['Resource Group', 'Keeps the UI, API, database, monitoring, and related resources together.'],
      ['App Service Plan', 'Provides the region, operating system, computing resources, and pricing tier for the web applications.'],
      ['Azure App Service', 'Hosts the MVC UI and Web API as separate web applications with public HTTPS URLs.'],
      ['Azure SQL Database', 'Stores books, users, carts, orders, and other application data.'],
      ['Application Insights', 'Tracks requests, exceptions, response times, and dependency failures.'],
      ['GitHub Actions', 'Builds, tests, and deploys the application after a code change.'],
    ], { widths: [1.7, 4.3] }),

    B.h1('Target Architecture'),
    B.p('The UI and API are deployed separately. The browser calls the MVC UI, the UI calls the API, and the API reads and writes data in Azure SQL Database.'),
    B.table(['Component', 'Azure Hosting Location', 'Responsibility'], [
      ['MVC UI', 'App Service - bookshoppingcart-ui', 'Displays books, cart, login, and checkout pages.'],
      ['Web API', 'App Service - bookshoppingcart-api', 'Handles authentication, business rules, and database operations.'],
      ['Database', 'Azure SQL Database', 'Stores application data.'],
    ], { widths: [1.2, 2.6, 2.2] }),

    B.h1('Deployment Steps'),
    B.numbered('Create a Resource Group, for example bookshoppingcart-rg, in a suitable Azure region.', 0, 1),
    B.numbered('Create an App Service Plan. The plan supplies the computing resources used by the UI and API.', 0, 1),
    B.numbered('Create Azure SQL Database. Configure the database firewall so the Web API can connect to it.', 0, 1),
    B.numbered('Create the API Web App. Add its production connection string and JWT settings through Environment variables.', 0, 1),
    B.numbered('Deploy the Web API. Open Swagger and test a public endpoint and login before continuing.', 0, 1),
    B.numbered('Create the MVC UI Web App. Set its API base URL to the deployed API URL.', 0, 1),
    B.numbered('Deploy the MVC UI. Test login, books, cart updates, and checkout using the hosted UI URL.', 0, 1),
    B.numbered('Enable Application Insights and review errors, slow requests, and database dependency calls.', 0, 1),

    B.h1('Configuration and Security'),
    B.p('Store production values in Azure App Service Environment variables instead of source code. Typical settings are ConnectionStrings__DefaultConnection, Jwt__Key, Jwt__Issuer, Jwt__Audience, and ApiSettings__BaseUrl.'),
    B.note('Azure cannot directly use the local SQL Server instance COGNINE-L218\\SQLEXPRESS01. Use Azure SQL Database or a database securely reachable from Azure.'),

    B.h1('Release and Verification'),
    B.table(['Check', 'Expected Result'], [
      ['Build and tests', 'GitHub Actions completes successfully before deployment.'],
      ['API verification', 'Swagger loads and the API can connect to Azure SQL Database.'],
      ['UI verification', 'Login, books, cart, and checkout workflows succeed.'],
      ['Monitoring', 'Application Insights and Log Stream show no unexpected errors.'],
      ['Rollback', 'Deploy the earlier release or use a deployment slot swap after slots are configured.'],
    ], { widths: [1.7, 4.3] }),
  ],
});

B.save(doc, 'Azure_App_Service_Deployment_Guide.docx');
