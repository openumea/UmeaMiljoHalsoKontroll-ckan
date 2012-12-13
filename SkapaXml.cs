using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Umea.se.MiljoHalsoKontroll.BusinessLogicLayer;
using Umea.se.MiljoHalsoKontroll.BusinessLogicLayer.Logic;
using Umea.se.MiljoHalsoKontroll.DataObjectLibrary;

namespace Umea.se.MiljoHalsoKontroll.PresentationLayer
{
	public class SkapaXml
	{
		private void uploadToCkan(string dataSet, string fileName, string filePath)
		{
			#region uploadToCkan
			//Hämtar värden från webconfig
			string apiUrl = System.Configuration.ConfigurationManager.AppSettings["apiUrl"];
			string apiToken = System.Configuration.ConfigurationManager.AppSettings["apiToken"];

			string dateTimeNow = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
			// Prefixa filename med nuvarande datum för att matcha hur ckan gör uploads.
			fileName = dateTimeNow + "/" + fileName;
			string authUrl = apiUrl + "/storage/auth/form/" + fileName;
			string fileMetadataUrl = apiUrl + "/storage/metadata/" + fileName;
			string resourceCreateUrl = apiUrl + "/action/resource_create";

			//Ta reda på vart vi ska posta informationen och med vilka credentails
			WebRequest objWebRequest;
			objWebRequest = WebRequest.Create(authUrl);
			objWebRequest.Method = "GET";
			objWebRequest.Headers.Add("Authorization: " + apiToken);
			HttpWebResponse objHttpWebResponse;
			objHttpWebResponse = (HttpWebResponse)objWebRequest.GetResponse();
			StreamReader streamReader = new StreamReader(objHttpWebResponse.GetResponseStream());
			StorageAuthFormResponse storageAuth = JsonConvert.DeserializeObject<StorageAuthFormResponse>(streamReader.ReadToEnd());

			//Skapa en multipart/form-data med credentails och själva filen
			objWebRequest = WebRequest.Create(storageAuth.action);
			objWebRequest.Method = "POST";
			//En unik boundery för att separera de olika blocken i form-data'n
			string boundary = "40C406D7-DBC9-4317-B4C8-D0EF05931341";
			objWebRequest.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);

			//Bygg själva form-data-blocket med credentails
			StringBuilder sb = new StringBuilder();
			foreach (StorageAuthFormResponseField item in storageAuth.fields)
			{
				sb.AppendFormat("--{0}", boundary);
				sb.AppendFormat("\r\n");
				sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"", item.name);
				sb.AppendFormat("\r\n");
				sb.AppendFormat("\r\n");
				sb.AppendFormat(item.value);
				sb.AppendFormat("\r\n");
			}
			sb.AppendFormat("--{0}", boundary);
			sb.AppendFormat("\r\n");

			//Lägg in själva filen
			sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"", "file", fileName);
			sb.AppendFormat("\r\n");
			sb.AppendFormat("\r\n");

			StreamReader xmlReader = File.OpenText(filePath);
			string input = null;
			while ((input = xmlReader.ReadLine()) != null)
			{
				sb.AppendFormat(input + "\n");
			}
			xmlReader.Close();
			sb.AppendFormat("\r\n");
			sb.AppendFormat("--{0}--", boundary);

			//Lägg in hela form-datan i anropet
			byte[] fulldata = Encoding.UTF8.GetBytes(sb.ToString());
			objWebRequest.ContentLength = fulldata.Length;

			Stream newStream = objWebRequest.GetRequestStream();
			newStream.Write(fulldata, 0, fulldata.Length);
			newStream.Close();

			//Skicka upp filen med credentails
			WebResponse response = objWebRequest.GetResponse();

			//Hämta informationen om den nu uppladdade filen
			objWebRequest = WebRequest.Create(fileMetadataUrl);
			objWebRequest.Method = "GET";
			objWebRequest.Headers.Add("Authorization: " + apiToken);
			response = objWebRequest.GetResponse();
			streamReader = new StreamReader(response.GetResponseStream());
			StorageMetaDataResponse storageMetaDataResponse = JsonConvert.DeserializeObject<StorageMetaDataResponse>(streamReader.ReadToEnd());

			//Registrera filen i CKAN
			objWebRequest = WebRequest.Create(resourceCreateUrl);
			objWebRequest.Method = "POST";
			objWebRequest.ContentType = "application/json";
			objWebRequest.Headers.Add("Authorization: " + apiToken);

			ActionResourceCreate arc = new ActionResourceCreate();
			arc.package_id = dataSet;
			arc.name = fileName;
			arc.size = storageMetaDataResponse._content_length;
			arc.url = storageMetaDataResponse._location;
			arc.hash = storageMetaDataResponse._checksum;
			// Vi vet att den här koden bara laddar upp xml-filer,
			// men abstrahera ut det här när den laddar upp andra saker
			arc.format = "xml";
			arc.mimetype = "application/xml";
			arc.resource_type = "file.upload";

			fulldata = Encoding.Default.GetBytes(JsonConvert.SerializeObject(arc));
			objWebRequest.ContentLength = fulldata.Length;
			newStream = objWebRequest.GetRequestStream();
			newStream.Write(fulldata, 0, fulldata.Length);
			newStream.Close();
			objWebRequest.GetResponse();
			#endregion
		}

		public void skapaXmlFranLista(string typAvLista)
		{
			string connStr = ConfigurationManager.ConnectionStrings["DbConnStrTest"].ConnectionString;
			// Defineras i blocken nedan och återanvänds i slutet av metoden
			string dataSet;
			string fileName;
			XmlSerializer serializer;

			if (typAvLista == "livsmedel")
			{
				#region Livsmedel
				//Lägg dessa i webconfig
				dataSet = System.Configuration.ConfigurationManager.AppSettings["dataSetLivsmedel"];
				fileName = System.Configuration.ConfigurationManager.AppSettings["fileNameLivs"];

				//Skapa instans av klassen Umea_ObjectInspektionVyLogic
				Umea_ObjektInspektionVyLogic listaSok = new Umea_ObjektInspektionVyLogic();

				//Skapa lista och hämta resultat för den
				List<Umea_ObjektInspektionVy> listan = new List<Umea_ObjektInspektionVy>();
				listan = listaSok.returneraSvar(connStr, "samtliga", "");

				// Skapa instans av XmlSerializer för just den här listan
				serializer = new XmlSerializer(listan.GetType());
				#endregion
			}
			else if (typAvLista == "radon")
			{
				#region Radon
				//Lägg dessa i webconfig
				dataSet = System.Configuration.ConfigurationManager.AppSettings["dataSetRadon"];
				fileNameRadon = System.Configuration.ConfigurationManager.AppSettings["fileNameRadon"];

				//Skapa instans av klassen Umea_ObjectInspektionVyLogic
				Umea_RadonInspektionVyLogic listaSok = new Umea_RadonInspektionVyLogic();

				//Skapa lista och hämta resultat för den
				List<Umea_RadonInspektionVy> listan = new List<Umea_RadonInspektionVy>();
				listan = listaSok.SelectLikeAdressOrFastighet(connStr, "samtliga", "");

				//Skapa instans av XmlSerializer för just den här listan
				serializer = new XmlSerializer(listan.GetType());
				#endregion
			}
			else
			{
				throw new Exception("Okänd listtyp, ({0}), begärd", typAvLista);
			}
			//Peka ut vart filen ska ligga
			string xmlPath = HttpContext.Current.Request.MapPath("~/XML/" + fileName);
			TextWriter textWriter = new StreamWriter(xmlPath);

			//Serialisera listan till XML
			serializer.Serialize(textWriter, listan);
			textWriter.Close();

			//Lägg till skapatdatum i xml:en
			XmlDocument docXml = new XmlDocument();
			docXml.Load(xmlPath);
			XmlElement element = docXml.DocumentElement;
			element.SetAttribute("Created", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"));
			docXml.Save(xmlPath);
			textWriter.Close();

			uploadToCkan(dataSet, fileName, xmlPath);
			//FIXME: Ta bort den temporära filen, xmlPath?
		}
	}
}
