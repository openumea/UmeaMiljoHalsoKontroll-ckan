using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using System.IO;
using Umea.se.MiljoHalsoKontroll.DataObjectLibrary;
using System.Configuration;
using Umea.se.MiljoHalsoKontroll.BusinessLogicLayer;
using System.Xml;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using System.Web.UI.WebControls;
using System.Web.UI;
using Umea.se.MiljoHalsoKontroll.BusinessLogicLayer.Logic;

namespace Umea.se.MiljoHalsoKontroll.PresentationLayer
{
    public class SkapaXml
    {
        public void skapaXmlFranLista(string typAvLista)
        {
            #region Livsmedel
            if (typAvLista == "livsmedel")
            {
                string connStr = ConfigurationManager.ConnectionStrings["DbConnStrTest"].ConnectionString;
                //Skapa instans av klassen Umea_ObjectInspektionVyLogic
                Umea_ObjektInspektionVyLogic listaSok = new Umea_ObjektInspektionVyLogic();

                //Skapa lista och hämta resultat för den
                List<Umea_ObjektInspektionVy> listan = new List<Umea_ObjektInspektionVy>();
                listan = listaSok.returneraSvar(connStr, "samtliga", "");

                // Skapa instans av XmlSerializer
                XmlSerializer serializer = new XmlSerializer(listan.GetType());

                //Peka ut vart filen ska ligga
                string xmlPath = HttpContext.Current.Request.MapPath("~/XML/inspektionerLivs.xml");
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

                //Hämtar värden från webconfig
                string apiUrl = System.Configuration.ConfigurationManager.AppSettings["apiUrl"];
                string dataSet = System.Configuration.ConfigurationManager.AppSettings["dataSetLivsmedel"];
                string apiToken = System.Configuration.ConfigurationManager.AppSettings["apiToken"];
                string fileNameLivs = System.Configuration.ConfigurationManager.AppSettings["fileNameLivs"];
                
                string dateTimeNow = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
                fileNameLivs = dateTimeNow + "/" + fileNameLivs;
                string authUrl = apiUrl + "/storage/auth/form/" + fileNameLivs;
                string fileMetadataUrl = apiUrl + "/storage/metadata/" + fileNameLivs;
                string resourceCreateUrl = apiUrl + "/action/resource_create";

                WebRequest objWebRequest;
                objWebRequest = WebRequest.Create(authUrl);
                objWebRequest.Method = "GET";
                objWebRequest.Headers.Add("Authorization: " + apiToken);
                HttpWebResponse objHttpWebResponse;
                objHttpWebResponse = (HttpWebResponse)objWebRequest.GetResponse();
                StreamReader streamReader = new StreamReader(objHttpWebResponse.GetResponseStream());
                storageAuthFormResponse storageAuth = JsonConvert.DeserializeObject<storageAuthFormResponse>(streamReader.ReadToEnd());

                //Det första vi får är action för vart vi ska posta data
                objWebRequest = WebRequest.Create(storageAuth.action);
                objWebRequest.Method = "POST";
                string boundary = "40C406D7-DBC9-4317-B4C8-D0EF05931341";
                objWebRequest.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);

                StringBuilder sb = new StringBuilder();

                foreach (storageAuthFormResponseField item in storageAuth.fields)
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
                sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"", "file", fileNameLivs);
                sb.AppendFormat("\r\n");
                sb.AppendFormat("\r\n");

                StreamReader xmlReader = File.OpenText(xmlPath);
                string input = null;
                while ((input = xmlReader.ReadLine()) != null)
                {
                    sb.AppendFormat(input + "\n");
                }
                sb.AppendFormat("\r\n");
                xmlReader.Close();

                sb.AppendFormat("--{0}--", boundary);
                //Lägg till xml till REST-anrop
                byte[] fulldata = Encoding.UTF8.GetBytes(sb.ToString());
                objWebRequest.ContentLength = fulldata.Length;

                Stream newStream = objWebRequest.GetRequestStream();
                newStream.Write(fulldata, 0, fulldata.Length);
                newStream.Close();

                //Ladda upp filen
                WebResponse response = objWebRequest.GetResponse();

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
                arc.name = fileNameLivs;
                arc.size = storageMetaDataResponse._content_length;
                arc.url = storageMetaDataResponse._location;
                arc.hash = storageMetaDataResponse._checksum;
                arc.format = "xml";
                arc.mimetype = "application/xml";
                arc.resource_type = "file.upload";

                fulldata = Encoding.Default.GetBytes(JsonConvert.SerializeObject(arc));
                objWebRequest.ContentLength = fulldata.Length;
                newStream = objWebRequest.GetRequestStream();
                newStream.Write(fulldata, 0, fulldata.Length);
                newStream.Close();
                objWebRequest.GetResponse();
            }
            #endregion

            #region Radon
            if (typAvLista == "radon")
            {
                string connStr = ConfigurationManager.ConnectionStrings["DbConnStrTest"].ConnectionString;
                //Skapa instans av klassen Umea_ObjectInspektionVyLogic
                Umea_RadonInspektionVyLogic listaSok = new Umea_RadonInspektionVyLogic();

                //Skapa lista och hämta resultat för den
                List<Umea_RadonInspektionVy> listan = new List<Umea_RadonInspektionVy>();
                listan = listaSok.SelectLikeAdressOrFastighet(connStr, "samtliga", "");

                //Skapa instans av XmlSerializer
                XmlSerializer serializer = new XmlSerializer(listan.GetType());

                //Peka ut vart filen ska sparas
                string xmlPath = HttpContext.Current.Request.MapPath("~/XML/inspektionerRadon.xml");
                TextWriter textWriter = new StreamWriter(xmlPath);

                //Serialisera listan
                serializer.Serialize(textWriter, listan);
                textWriter.Close();

                //Lägg till skapatdatum i xml:en
                XmlDocument docXml = new XmlDocument();
                docXml.Load(xmlPath);
                XmlElement element = docXml.DocumentElement;
                element.SetAttribute("Created", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"));
                docXml.Save(xmlPath);
                textWriter.Close();

                //Lägg dessa i webconfig
                string apiUrl = System.Configuration.ConfigurationManager.AppSettings["apiUrl"];
                string dataSet = System.Configuration.ConfigurationManager.AppSettings["dataSetRadon"];
                string apiToken = System.Configuration.ConfigurationManager.AppSettings["apiToken"];
                string fileNameRadon = System.Configuration.ConfigurationManager.AppSettings["fileNameRadon"];

                string dateTimeNow = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
                fileNameRadon = dateTimeNow + "/" + fileNameRadon;
                string authUrl = apiUrl + "/storage/auth/form/" + fileNameRadon;
                string fileMetadataUrl = apiUrl + "/storage/metadata/" + fileNameRadon;
                string resourceCreateUrl = apiUrl + "/action/resource_create";

                WebRequest objWebRequest;
                objWebRequest = WebRequest.Create(authUrl);
                objWebRequest.Method = "GET";
                objWebRequest.Headers.Add("Authorization: " + apiToken);
                HttpWebResponse objHttpWebResponse;
                objHttpWebResponse = (HttpWebResponse)objWebRequest.GetResponse();
                StreamReader streamReader = new StreamReader(objHttpWebResponse.GetResponseStream());
                storageAuthFormResponse storageAuth = JsonConvert.DeserializeObject<storageAuthFormResponse>(streamReader.ReadToEnd());

                //Det första vi får är action för vart vi ska posta data
                objWebRequest = WebRequest.Create(storageAuth.action);
                objWebRequest.Method = "POST";
                string boundary = "40C406D7-DBC9-4317-B4C8-D0EF05931341";
                objWebRequest.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);

                StringBuilder sb = new StringBuilder();

                foreach (storageAuthFormResponseField item in storageAuth.fields)
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
                sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"", "file", fileNameRadon);
                sb.AppendFormat("\r\n");
                sb.AppendFormat("\r\n");

                StreamReader xmlReader = File.OpenText(xmlPath);

                string input = null;
                while ((input = xmlReader.ReadLine()) != null)
                {
                    sb.AppendFormat(input + "\n");
                }
                sb.AppendFormat("\r\n");
                xmlReader.Close();

                sb.AppendFormat("--{0}--", boundary);
                //Lägg till xml till REST-anrop
                byte[] fulldata = Encoding.UTF8.GetBytes(sb.ToString());
                objWebRequest.ContentLength = fulldata.Length;

                Stream newStream = objWebRequest.GetRequestStream();
                newStream.Write(fulldata, 0, fulldata.Length);
                newStream.Close();

                //Ladda upp filen
                WebResponse response = objWebRequest.GetResponse();

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
                arc.name = fileNameRadon;
                arc.size = storageMetaDataResponse._content_length;
                arc.url = storageMetaDataResponse._location;
                arc.hash = storageMetaDataResponse._checksum;
                arc.format = "xml";
                arc.mimetype = "application/xml";
                arc.resource_type = "file.upload";

                fulldata = Encoding.Default.GetBytes(JsonConvert.SerializeObject(arc));
                objWebRequest.ContentLength = fulldata.Length;
                newStream = objWebRequest.GetRequestStream();
                newStream.Write(fulldata, 0, fulldata.Length);
                newStream.Close();
                objWebRequest.GetResponse();
            }
            #endregion
        }
    }
}
