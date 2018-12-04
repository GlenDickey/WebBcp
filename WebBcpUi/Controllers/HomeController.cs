using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using WebBcpModel;

namespace WebBcpUi.Controllers
{
	public class HomeController : Controller
	{
		private HttpClient setupHttpClient()
		{
			var http = new HttpClient();

			string serviceApiBaseUrl = WebBcpUi.Properties.Settings.Default.WebBcpServiceBaseAddress;

			http.BaseAddress = new Uri(serviceApiBaseUrl);

			http.DefaultRequestHeaders.Accept.Clear();

			http.DefaultRequestHeaders.Accept.Add(
				new MediaTypeWithQualityHeaderValue("application/json")
			);

			return http;
		}

		[HttpPost]
		public async Task<JsonResult> Upload()
		{
			#region upload files 

			// list of files copied up 
			List<string> filePathsUploaded= new List<string>();
	
			// iterate over all files and save to server
			for (int i = 0; i < Request.Files.Count; i++)
			{
				var file = Request.Files[i];

				var fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_@_" + Guid.NewGuid().ToString() +  Path.GetExtension(file.FileName);

				// proactivly replace these but the rest are up to you, probably abetter way to do this
				fileName = 
					fileName
						.Replace(" ", "_")
						.Replace("-", "_")
						.Replace("(", "_")
						.Replace(")", "_")
						.Replace("[", "_")
						.Replace("]", "_");

				var path = Path.Combine(Server.MapPath("~/Upload/"), fileName);
				file.SaveAs(path);

				// add to list to pass to component
				filePathsUploaded.Add(path);
			}

			#endregion

			// get values from FormData
			var formatType = Request.Params["FormatType"].ToString();
			var option = Request.Params["Option"].ToString();
			var singleTableName = Request.Params["SingleTableName"].ToString();

			#region get asynchronously 

			string json = "";

			if (Request.Files.Count > 0)
			{
				// setup client
				HttpClient http = setupHttpClient();

				// combine all parameters  
				var vm = 
					new WebBcpUploadModel()
					{
						FormatType = formatType,
						Option = option,
						SingleTableName = singleTableName,
						Files = filePathsUploaded,
						Log = new List<BcpLog>()
					};

				// make the call now!
				HttpResponseMessage response = await http.PostAsJsonAsync("api/WebBcpService/Post", vm);

				// .NET Core => HttpResponseMessage response = await http.PostAsync("api/WebBcpService/Post", vm, new System.Net.Http.Formatting.JsonMediaTypeFormatter());

				if (response.IsSuccessStatusCode)
				{
					var responseContent = response.Content;

					// get the answer
					json = await response.Content.ReadAsStringAsync();

				} else {
					json = "{ \"Message\": \"" + response.ReasonPhrase + "\" }";
				}
			}

			#endregion

			#region get synchronously 

			//// set service and check if ready 
			//var response = http.GetAsync("WebBcp/Get").Result;

			//if (response.IsSuccessStatusCode)
			//{
			//	var responseContent = response.Content;

			//	// get the data in json format
			//	string json = responseContent.ReadAsStringAsync().Result;

			//	// convert the json to view model from shared DataModel projects
			//	//model = JsonConvert.DeserializeObject<List<InfoVm>>(json);

			//	Console.WriteLine(json);
			//}

			#endregion

			return Json(json);
		}

		public ActionResult Index()
		{
			// setup client
			HttpClient http = setupHttpClient();

			List<NumericIdDupleType> model = new List<NumericIdDupleType>();

			// get list of format types from db
			var response = http.GetAsync("api/WebBcpService/Get").Result;

			if (response.IsSuccessStatusCode)
			{
				var responseContent = response.Content;
				string json = responseContent.ReadAsStringAsync().Result;

				model = JsonConvert.DeserializeObject<List<NumericIdDupleType>>(json);
			}

			return View(model);
		}

	}
}