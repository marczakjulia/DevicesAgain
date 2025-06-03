In my configuraition file (which let's forget i put here earlier) there is a

-> "ConnectionStrings" defined which is responsible for the defaultConnection.

-> Inside of it, there is a Data Source so the server address and port(seperated by a coma).

-> there is a user id and password, which allow me to access the database.

-> Lastly, the "TrustServerCertificate" which can be either true or false, but in my case is defined as true

Also in the settings there are Jwt settings which include

-> "Issuer" - who is responsible for tokens

->"Audience" - the recipient of the issuer (in my case its the same as the issuer and as shown during tutorials)

->"Key" - a random key which is safe 

->"ValidInMinutes" - for how many minutes the token i generate is valid. currently set to 10 minutes, meaning after 
generating the token we only have 10 minutes to use it when testing, otherwise we have to regenerate it
