# ADFS-Integration-Signing-Certificate-Extractor
Extracts the signing certificate for ADFS integrations from the FederationMetadata.xml
I put this together since I'm working with application with "ADFS integration" that can't handle to 
extract the certificates them selfs but require them to be imported on the client side.

## Use
ADFSCertExtractor.exe {ADFS url}
