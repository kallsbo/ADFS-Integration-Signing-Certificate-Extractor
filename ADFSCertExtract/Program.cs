using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using Mono.Options;

namespace ADFSCertExtract
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Vars
            string url = null;
            string outputpath = null;
            bool show_help = false;

            string federation_xml = "";
            string host = "";
            #endregion

            #region Set parameters
            var p = new OptionSet() {
              { "u|url=", "ADFS url (required)",
              v => url = v},
              {"o|output=", "Output path for certificate (required)",
              v => outputpath = v},
              { "h|help",  "show this message and exit",
              v => show_help = v != null },
            };
            #endregion

            #region Check parameters
            try
            {
                // Try parse
                p.Parse(args);

                // Check if help should be displayed
                if (show_help)
                {
                    ShowHelp(p);
                    return;
                }

                // Check required parameters
                if (url == null)
                    throw new OptionException("ADFS url is required!", "u|url=");

                if (outputpath == null)
                    throw new OptionException("Output path for certificate is required!", "o|output=");

                // Check that output path excists
                if (!Directory.Exists(outputpath))
                    throw new OptionException("Output path doesn't excist or is not accesible!", "o|output=");

                // Get host from url
                Uri adfsUri = new Uri(url);
                host = adfsUri.Host;
            }
            catch (OptionException ex)
            {
                Console.WriteLine(string.Format("Error: {0}", ex.Message));
                Console.WriteLine();
                ShowHelp(p);
                return;

            }
            #endregion

            #region Download Cert
            // Check for complete url
            if (!url.ToLower().Contains("/federationmetadata/2007-06/federationmetadata.xml"))
                url = url + "/FederationMetadata/2007-06/FederationMetadata.xml";

            // Try download
            try
            {
                WebClient client = new WebClient();
                federation_xml = client.DownloadString(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Unable to reach url, check the url and try again!");
                Console.WriteLine("Msg: " + ex.Message);
                return;
            }
            #endregion

            #region Read xml
            try
            {
                // Load XML
                var document = new XmlDocument();
                document.LoadXml(federation_xml);

                // Set root and namespace
                XmlNode root = document.DocumentElement;
                XmlNamespaceManager NS = new XmlNamespaceManager(document.NameTable);
                NS.AddNamespace("default", "urn:oasis:names:tc:SAML:2.0:metadata");
                NS.AddNamespace("keys", "http://www.w3.org/2000/09/xmldsig");
                NS.AddNamespace("keys1", "http://www.w3.org/2000/09/xmldsig#");

                // Get cert node
                XmlNodeList cert = root.SelectNodes("descendant::keys1:X509Certificate", NS);

                // Loop the certs
                foreach (XmlNode thisNode in cert)
                {
                    string thisText = thisNode.InnerText;
                    byte[] keydata = Convert.FromBase64String(thisText);
                    var x509c = new X509Certificate2(keydata);

                    // Write cert to disk
                    string outputfile = string.Format(@"{0}\{1}-signing.cer", outputpath, host);
                    File.WriteAllText(outputfile, Convert.ToBase64String(x509c.Export(X509ContentType.Cert)));
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("ERROR: Writing certificate, check output path!");
                Console.WriteLine("Msg: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
            #endregion
        }

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Extract the signing cert from an ADFS federation service.");
            Console.WriteLine("Usage: ace.exe [OPTIONS]");
            Console.WriteLine("");
            p.WriteOptionDescriptions(Console.Out);
            Console.WriteLine("");
            Console.WriteLine("Kristofer Källsbo 2017");
        }
    }
}
