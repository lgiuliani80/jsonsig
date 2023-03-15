# JSON signer / verifies

## Key creation

### Generate the private key

Call:
     
    openssl ecparam -name brainpoolP160r1 -genkey -noout -out mykeyfile.pem

the private key will be stored in the `mykeyfile.pem`.


### Extract the public key

Call:
     
    openssl ec -in mykeyfile.pem -pubout -out mykeyfile.pub.pem

the private key will be stored in the `mykeyfile.pub.pem`.


## Usage

* Sign a JSON file:

      ./jsonsig sign <json-file-to-sign>

  the signed JSON will be emitted as standard output of the program.

  **NOTES**: 
  - the supplied `.json` file needs to be a Json Object at top level, so it must begin with `{` and and with `}`;
  - the `appsettings.json` needs to contain the `"PrivateKeyFileDf"` key, pointing to the .pem file containing the __Private Key__.
  - the `"KeySpec"` setting represents a short identifier of the private key used, should more than one key be used (and to address key rotation in the future). Keep this string as short as possible, e.g. 2 characters.

* Verify a signed JSON file:

      ./jsonsig verify <json-file-to-verify>

  the result of the check will be printed in standard output.

  **NOTES**: 
  - the supplied `.json` file needs to be a Json Signed Object i.e. a Json Object containing the "sig" string member;
  - the `appsettings.json` needs to contain the `"PublicKeyFileDf"` key, pointing to the .pem file containing the __Public Key__ **OR** `"PrivateKeyFileDf"` key, pointing to the .pem file containing the __Private Key__.
  - the key specified contained in the `"sig"` member of the JSON object will be compared against the `"KeySpec"` setting contains in `appsettings.json`.
