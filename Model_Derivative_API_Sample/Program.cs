using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Forge;
using Autodesk.Forge.Client;
using Autodesk.Forge.Model;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp.Extensions.MonoHttp;
using RestSharp;
using System.Configuration;

namespace Model_Derivative_API_Sample
{
    class Program
    {
        // you can also hardcode them in the code if you want in the placeholders below
        private static string FORGE_CLIENT_ID = Environment.GetEnvironmentVariable("FORGE_CLIENT_ID") ?? ConfigurationManager.AppSettings["FORGE_CLIENT_ID"];
        private static string FORGE_CLIENT_SECRET = Environment.GetEnvironmentVariable("FORGE_CLIENT_SECRET") ?? ConfigurationManager.AppSettings["FORGE_CLIENT_SECRET"];
        private static Scope[] _scope = new Scope[] { Scope.DataRead, Scope.DataWrite };
        // you can also hardcode them in the code if you want in the placeholders below
        private static string BUCKET_KEY = "au-sample-bucket-1" + FORGE_CLIENT_ID.ToLower();
        private static string FILE_NAME = "Motor.f3d";
        private static string FILE_PATH = "Motor.f3d";
        // Initialize the relevant clients; in this example, the Objects, Buckets and Derivatives clients, which are part of the Data Management API and Model Derivatives API
        private static BucketsApi bucketsApi = new BucketsApi();
        private static ObjectsApi objectsApi = new ObjectsApi();
        private static DerivativesApi derivativesApi = new DerivativesApi();

        private static TwoLeggedApi oauth2TwoLegged;
        private static dynamic twoLeggedCredentials;

        // Intialize the 2-legged oAuth 2.0 client.
        private static TwoLeggedApi _twoLeggedApi = new TwoLeggedApi();
        private static ObjectsApi _objectsApi = new ObjectsApi();
        private static string base64Urn = "";
        private static dynamic manifest;

        static void Main(string[] args)
        {
            //Add the FORGE_CLIENT_ID and  FORGE_CLIENT_SECRET to App Config.

            initializeOAuth();

            createBucket();

            dynamic uploadedObject = uploadFile();
            dynamic job = translateToSVF(uploadedObject.objectId);

            // Translate the source file to STEP file format
            translateToStep(base64Urn);

            //Translate the source file to OBJ file format
            translateToOBJ(base64Urn);

            extractMetadataAndProperties(base64Urn);

            /* Sample code for extracting geometry.
            
            var selectedObjectId = new List<int>();
            selectedObjectId.Add(5); // Specify the object id of the selected part.
            string modelGuid = "4f981e94-8241-4eaf-b08b-cd337c6b8b1f"; // Specify the model view guid in which the part exists.
            extractGeometry(base64Urn, modelGuid, selectedObjectId);

            */

        }

        private static void initializeOAuth()
        {
            // You must provide at least one valid scope
            Scope[] scopes = new Scope[] { Scope.DataRead, Scope.DataWrite, Scope.BucketCreate, Scope.BucketRead };
            oauth2TwoLegged = new TwoLeggedApi();
            twoLeggedCredentials = oauth2TwoLegged.Authenticate(FORGE_CLIENT_ID /*Replace with your Client ID*/, FORGE_CLIENT_SECRET/*Replace with your Client Secret*/, oAuthConstants.CLIENT_CREDENTIALS, scopes);
            bucketsApi.Configuration.AccessToken = twoLeggedCredentials.access_token;
            objectsApi.Configuration.AccessToken = twoLeggedCredentials.access_token;
            derivativesApi.Configuration.AccessToken = twoLeggedCredentials.access_token;
        }

        private static void createBucket()
        {
            Console.WriteLine("***** Sending createBucket request");
            PostBucketsPayload payload = new PostBucketsPayload(BUCKET_KEY, null, PostBucketsPayload.PolicyKeyEnum.Persistent);
            dynamic response = bucketsApi.CreateBucket(payload, "US");
            Console.WriteLine("***** Response for createBucket: " + response.ToString());
        }
        

        private static dynamic verifyJobComplete(string base64Urn)
        {
            Console.WriteLine("***** Sending getManifest request");
            while (true)
            {
                dynamic response = derivativesApi.GetManifest(base64Urn);
                if (hasOwnProperty(response, "progress") && response.progress == "complete")
                {
                    Console.WriteLine("***** Finished translating your file - status: " + response.status
                        + ", progress: " + response.progress);
                    return (response);
                }
                else
                {
                    Console.WriteLine("***** Haven't finished translating your file - status: " + response.status
                        + ", progress: " + response.progress);
                    Thread.Sleep(1000);
                }
            }
        }

        public static bool hasOwnProperty(dynamic obj, string name)
        {
            try
            {
                var test = obj[name];
                return (true);
            }
            catch (Exception)
            {
                return (false);
            }
        }

        private static void openViewer(string base64Urn)
        {
            Console.WriteLine("***** Opening SVF file in viewer with urn:" + base64Urn);
            string st = _html.Replace("__URN__", base64Urn).Replace("__ACCESS_TOKEN__", twoLeggedCredentials.access_token);
            System.IO.File.WriteAllText("viewer.html", st);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("viewer.html"));
        }
        
        private static bool translateToSVF(string urn)
        {
            try { 
            Console.WriteLine("***** Sending Derivative API translate request for SVF");
            JobPayloadInput jobInput = new JobPayloadInput(
                System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(urn))
            );
             List<JobPayloadItem> outputs = new List<JobPayloadItem>()
                {
                 new JobPayloadItem(
                   JobPayloadItem.TypeEnum.Svf,
                   new List<JobPayloadItem.ViewsEnum>()
                   {
                     JobPayloadItem.ViewsEnum._2d,
                     JobPayloadItem.ViewsEnum._3d
                   })
                };
                JobPayload job = new JobPayload(jobInput, new JobPayloadOutput(outputs));
            dynamic response = derivativesApi.Translate(job, true);
            Console.WriteLine("***** Response for Translating File to SVF: " + response.ToString());
            if (response.result == "success" || response.result == "created")
            {
                base64Urn = response.urn;
                dynamic manifest = verifyJobComplete(base64Urn);
                if (manifest.status == "success")
                {
                        openViewer(manifest.urn); /*Uncomment this line to view the file in viewer*/
                        return true;
                }
            }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error translating file : " + ex.Message);
            }
            return false;
        }

        private static bool translateToStep(string urn)
        {
            try
            {
                Console.WriteLine("***** Sending Derivative API translate request for STEP");
                JobPayloadInput jobInput = new JobPayloadInput(urn);
                List<JobPayloadItem> outputs = new List<JobPayloadItem>()
                {
                 new JobPayloadItem(
                   JobPayloadItem.TypeEnum.Step,
                   new List<JobPayloadItem.ViewsEnum>()
                   {
                     JobPayloadItem.ViewsEnum._2d,
                     JobPayloadItem.ViewsEnum._3d
                   })
                };
                JobPayload job = new JobPayload(jobInput, new JobPayloadOutput(outputs));
                dynamic response = derivativesApi.Translate(job, true);
                Console.WriteLine("***** Response for Translating File to STEP " + response.ToString());

                if (response.result == "success" || response.result == "created")
                {
                    base64Urn = response.urn;
                    dynamic manifest = verifyJobComplete(base64Urn);
                    if (manifest.status == "success")
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error translating file : " + ex.Message);
            }
            return false;

        }

        private static bool translateToIFC(string urn)
        {
            try
            {
                Console.WriteLine("***** Sending Derivative API translate request for IFC");
                JobPayloadInput jobInput = new JobPayloadInput(urn);
                List<JobPayloadItem> outputs = new List<JobPayloadItem>()
                {
                 new JobPayloadItem(
                   JobPayloadItem.TypeEnum.IFC,
                   new List<JobPayloadItem.ViewsEnum>()
                   {
                     JobPayloadItem.ViewsEnum._2d,
                     JobPayloadItem.ViewsEnum._3d
                   })
                };
                JobPayload job = new JobPayload(jobInput, new JobPayloadOutput(outputs));
                dynamic response = derivativesApi.Translate(job, true);
                Console.WriteLine("***** Response for Translating File to IFC : " + response.ToString());

                if (response.result == "success" || response.result == "created")
                {
                    base64Urn = response.urn;
                    manifest = verifyJobComplete(base64Urn);
                    if (manifest.status == "success")
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error translating file : " + ex.Message);
            }
            return false;

        }

        private static bool translateToOBJ(string urn)
        {
            try
            {
                Console.WriteLine("***** Sending Derivative API translate request for OBJ");
                JobPayloadInput jobInput = new JobPayloadInput(urn);
                List<JobPayloadItem> outputs = new List<JobPayloadItem>()
                {
                 new JobPayloadItem(
                   JobPayloadItem.TypeEnum.Obj,
                   new List<JobPayloadItem.ViewsEnum>()
                   {
                     JobPayloadItem.ViewsEnum._2d,
                     JobPayloadItem.ViewsEnum._3d
                   })
                };
                JobPayload job = new JobPayload(jobInput, new JobPayloadOutput(outputs));
                dynamic response = derivativesApi.Translate(job, true);
                Console.WriteLine("***** Response for Translating File to OBJ: " + response.ToString());

                if (response.result == "success" || response.result == "created")
                {
                    base64Urn = response.urn;
                    dynamic manifest = verifyJobComplete(base64Urn);
                    if (manifest.status == "success")
                    {
                        Console.WriteLine("**** Successfully translated the file ");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error translating file : " + ex.Message);
            }
            return false;

        }

        
        private static void downloadDerivative(string urn, string format, dynamic manifest,List<int> objectids,string fileName)
        {
            DynamicDictionary derivatives = manifest.derivatives;
            foreach (dynamic derItems in derivatives.Items())
            {
                var childDerivatives = derItems.Value;
                foreach (dynamic child in childDerivatives.children.Items())
                {
                    
                    var childItems = child.Value;
                    if (childItems.role.Equals(format) )
                    {
                        string derUrns = string.Empty;
                        List<int> objIds = new List<int>();

                        if (hasOwnProperty(childItems, "objectIds"))
                        {
                            var x = childItems.objectIds;
                            DynamicDictionary z = childItems.objectIds;
                            foreach (dynamic key in z.Items())
                            {
                                objIds.Add(Convert.ToInt32(key.Value));
                            }
                            if (Enumerable.SequenceEqual(objIds, objectids))
                            {
                                 derUrns = childItems.urn;
                            }
                        }
                        else
                        {
                            derUrns = childItems.urn;
                            
                        }
                        if (!string.IsNullOrEmpty(derUrns) )
                        {

                            Console.WriteLine("***** Sending Derivative API Download request");
                           
                            derUrns = HttpUtility.UrlEncode(derUrns);

                            var md = "modelderivative/v2/designdata";
                            var url = string.Format("/{0}/{1}/manifest/{2}", md, base64Urn, derUrns);
                            var client = new RestClient(derivativesApi.GetBasePath());

                            var request = new RestRequest(url, Method.GET);
                            request.AddHeader("Authorization", "Bearer " + derivativesApi.Configuration.AccessToken);
                            request.AddHeader("Accept", "application/octet-stream");
                            request.AddParameter("content-type", "application/json");

                            IRestResponse response = client.Execute(request);
                            string filePath = @"..\..\" + fileName;
                            File.WriteAllBytes(filePath, response.RawBytes);
                            Console.WriteLine("***** Response for Downloading Derivative: " + response.ToString());

                        }
                    }
                }
            }
        }

        private static bool extractGeometry(string urn, string modelGuid, List<int> selectedObjectIds)
        {
            try
            {
                JobPayloadInput jobInput = new JobPayloadInput(urn);
                List<JobPayloadItem> outputs = new List<JobPayloadItem>()
                {
                 new JobPayloadItem(
                   JobPayloadItem.TypeEnum.Obj,
                   new List<JobPayloadItem.ViewsEnum>()
                   {
                     JobPayloadItem.ViewsEnum._2d,
                     JobPayloadItem.ViewsEnum._3d
                   },
                   new JobObjOutputPayloadAdvanced(){ModelGuid=modelGuid, ObjectIds=selectedObjectIds,ExportFileStructure=JobObjOutputPayloadAdvanced.ExportFileStructureEnum.Single })
                   };
                
                JobPayload job = new JobPayload(jobInput, new JobPayloadOutput(outputs));
                dynamic response = derivativesApi.Translate(job, true);
                Console.WriteLine("***** Response for Extracting File to OBJ: " + response.ToString());

                if (response.result == "success" || response.result == "created")
                {
                    base64Urn = response.urn;
                    dynamic manifest = verifyJobComplete(base64Urn);
                    if (manifest.status == "success")
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error translating file : " + ex.Message);
            }
            return false;
        }

        private static void extractMetadataAndProperties(string urn)
        {
           dynamic metadata =  derivativesApi.GetMetadata(urn);
            foreach (KeyValuePair<string, dynamic> metadataItem in new DynamicDictionaryItems(metadata.data.metadata))
            {
                dynamic hierarchy =  derivativesApi.GetModelviewMetadata(urn, metadataItem.Value.guid); // Note the response could  be 202 , followed by 200.  
                
                dynamic properties =  derivativesApi.GetModelviewProperties(urn, metadataItem.Value.guid); //// Note the response could  be 202 , followed by 200. 

                Console.WriteLine(JsonConvert.SerializeObject(hierarchy, Formatting.Indented));
                Console.WriteLine(JsonConvert.SerializeObject(properties, Formatting.Indented));
               
            }
        }
        
        private static dynamic uploadFile()
        {

            Console.WriteLine("***** Sending uploadFile request");
            string path = FILE_PATH;
            if (!File.Exists(path))
                path = @"..\..\" + FILE_PATH;
            using (StreamReader streamReader = new StreamReader(path))
            {
                dynamic uploadedObject = objectsApi.UploadObject(BUCKET_KEY,
                    FILE_NAME, (int)streamReader.BaseStream.Length, streamReader.BaseStream,
                    "application/octet-stream");
                Console.WriteLine("***** Response for uploadFile: ");
                Console.WriteLine("Uploaded object Details - Location: " + uploadedObject.location
                    + ", Size: " + uploadedObject.size);
                return (uploadedObject);
            }
        }

        #region Html
        private static readonly string _html = @"<!DOCTYPE html>
<html>
<head>
	<meta charset=""UTF-8"">
	<script src=""https://developer.api.autodesk.com/viewingservice/v1/viewers/three.min.css""></script>
	<link rel=""stylesheet"" href=""https://developer.api.autodesk.com/viewingservice/v1/viewers/style.min.css"" />
	<script src=""https://developer.api.autodesk.com/viewingservice/v1/viewers/viewer3D.min.js""></script>
</head>
<body onload=""initialize()"">
<div id=""viewer"" style=""position:absolute; width:90%; height:90%;""></div>
<script>
	function authMe () { return ('__ACCESS_TOKEN__') ; }

	function initialize () {
		var options ={
			'document' : ""urn:__URN__"",
			'env': 'AutodeskProduction',
			'getAccessToken': authMe
		} ;
		var viewerElement =document.getElementById ('viewer') ;
		//var viewer =new Autodesk.Viewing.Viewer3D (viewerElement, {}) ; / No toolbar
		var viewer =new Autodesk.Viewing.Private.GuiViewer3D (viewerElement, {}) ; // With toolbar
		Autodesk.Viewing.Initializer (options, function () {
			viewer.initialize () ;
			loadDocument (viewer, options.document) ;
		}) ;
	}
	function loadDocument (viewer, documentId) {
		// Find the first 3d geometry and load that.
		Autodesk.Viewing.Document.load (
			documentId,
			function (doc) { // onLoadCallback
				var geometryItems =[] ;
				geometryItems =Autodesk.Viewing.Document.getSubItemsWithProperties (
					doc.getRootItem (),
					{ 'type' : 'geometry', 'role' : '3d' },
					true
				) ;
				if ( geometryItems.length <= 0 ) {
					geometryItems =Autodesk.Viewing.Document.getSubItemsWithProperties (
						doc.getRootItem (),
						{ 'type': 'geometry', 'role': '2d' },
						true
					) ;
				}
				if ( geometryItems.length > 0 )
					viewer.load (
						doc.getViewablePath (geometryItems [0])//,
						//null, null, null,
						//doc.acmSessionId /*session for DM*/
					) ;
			},
			function (errorMsg) { // onErrorCallback
				alert(""Load Error: "" + errorMsg) ;
			}
		) ;
	}
</script>
</body>
</html>";

        #endregion

    }
}
