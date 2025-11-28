To create the AppWebApi

1. Create the database. With Terminal in folder _scripts 
   E.g. database name: sql-friends, database type: sqlserver, server: docker, default user: dbo, application: ../AppWebApi

   macOs
   ./database-rebuild-all.sh sql-friends sqlserver docker dbo ../AppWebApi
   ./database-rebuild-all.sh sql-friends mysql docker dbo ../AppWebApi
   ./database-rebuild-all.sh sql-friends postgresql docker dbo ../AppWebApi
   
   Windows
   ./database-rebuild-all.ps1 sql-friends sqlserver docker dbo ../AppWebApi
   ./database-rebuild-all.ps1 sql-friends mysql docker dbo ../AppWebApi
   ./database-rebuild-all.ps1 sql-friends postgresql docker dbo ../AppWebApi

   Ensure no errors from build, migration or database update


2. From Azure Data Studio you can now connect to the database
   Use connection string from user secrets:
   connection string corresponding to Tag
   "sql-friends.<db_type>.docker.root"

3. Use Azure Data Studio to execute SQL script DbContext/SqlScripts/<db_type>/azure/initDatabase.sql

4. Run AppWebApi with or without debugger

   Without debugger:   
   Open a Terminal in folder AppWebApi run: 
   dotnet run -lp https 
   open url: https://localhost:7066/swagger

   Verify your can execute endpoint Admin/Environment and Guest/Info

5. Use endpoint Admin/SeedUsers to seed users into the database  BEHÖVS INTE

6. Use endpoint Guest/LoginUser to login as dbo1 BEHÖVS INTE
{
  "userNameOrEmail": "dbo1",
  "password": "dbo1"
}

7. Authorize using Swagger Authorize button and paste in the encryptedToken recieved after login.
    NOTE!!: Copy and paste the encryptedToken WITHIN the quotation, i.e. WITHOUT the first and last quotation mark " BEHÖVS INTE?

8. Use endpoint Admin/Seed to seed the database, Admin/RemoveSeed to remove the seed
   Verify database seed with endpoint Guest/Info
   As dbo you can now use and play with all endpoints
