using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using WebBcpModel;
using WebBcpService.Data;

namespace WebBcpService.Controllers
{
	public class WebBcpServiceController : ApiController
	{
		#region support methods

		private List<BcpTableFormat> GetFormatTypes()
		{
			// get tables mappings from db
			var webBcpDb = new WebBcpService.Data.WebBcpDbEntities();

			var formatTypes =
				webBcpDb.BcpTableFormats
					.OrderBy(i => i.Name)
					.ToList();

			return formatTypes;
		}

		private List<BcpColumnFormat> GetColumnMappings(string formatType)
		{
			// get format column mappings from db
			var webBcpDb = new WebBcpService.Data.WebBcpDbEntities();

			var columnMappings =
				webBcpDb.BcpColumnFormats
					.Where(i => i.BcpTableFormat ==
						webBcpDb.BcpTableFormats
							.Where(j => j.Name == formatType)
							.FirstOrDefault()
					)
					.OrderBy(i => i.Sequence)
					.ToList();

			return columnMappings;
		}

		private async Task<int> BcpFile(string formatType, string filePath, string tableName, List<BcpColumnFormat> columnMappings)
		{
			int ret = -1;

			#region read in file to data table

			var fileLines = File.ReadAllLines(filePath);

			if (fileLines.Count() == 0)
			{
				return  0;
			}

			// create datatable clas for the file contents
			var table = new DataTable();

			// create each column
			foreach (var cm in columnMappings)
			{
				table.Columns.Add(cm.FromColumnName);
			}

			// create each row in datatable
			for (int loop = 0; loop < fileLines.Count() - 1; loop++)
			{
				table.Rows.Add(
					fileLines[loop].Split(',')
				);
			}

			#endregion

			#region use bulk copy to import rows

			var connectionString = ConfigurationManager.ConnectionStrings["WebBcp"].ConnectionString;

			using (SqlConnection sqlConnection = new SqlConnection(connectionString))
			{				
				var sqlBulk = 
					new SqlBulkCopy(sqlConnection)
					{
						BatchSize = 10000,
						BulkCopyTimeout = 0
					};

				sqlBulk.DestinationTableName = tableName;
				sqlBulk.ColumnMappings.Clear();

				foreach (var cm in columnMappings)
				{
					if (cm.ToColumnName != "")
					{
						sqlBulk.ColumnMappings.Add(cm.FromColumnName, cm.ToColumnName);
					}
				}

				sqlConnection.Open();
				sqlBulk.WriteToServer(table);
			}

			#endregion

			ret = table.Rows.Count;

			return ret;
		}

		private bool SqlTableExists(string singleTableName)
		{
			// determine if table already exists
			var connectionString = ConfigurationManager.ConnectionStrings["WebBcp"].ConnectionString;

			string sql = "select count(1) from sysobjects where type in ('U', 'V', 'IT', 'S') and name = '" + singleTableName + "'";

			int tableCount = -1;

			using (SqlConnection sqlConnection = new SqlConnection(connectionString))
			{
				using (SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection))
				{
					sqlCmd.CommandType = CommandType.Text;
					sqlConnection.Open();
					tableCount = Convert.ToInt32(sqlCmd.ExecuteScalar());
				}
			}

			return (tableCount > 0); 
		}

		private bool SqlCreateTable(string sql)
		{
			// execute non query sql creating table
			var connectionString = ConfigurationManager.ConnectionStrings["WebBcp"].ConnectionString;

			using (SqlConnection sqlConnection = new SqlConnection(connectionString))
			{
				using (SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection))
				{
					sqlCmd.CommandType = CommandType.Text;
					sqlCmd.Connection.Open();
					sqlCmd.ExecuteNonQuery();
				}
			}

			return true;
		}

		private string BuildTable(string formatType, string name)
		{
			// simple message describing result
			string ret = "UNFINISHED";

			try
			{
				// create table based on format specified
				var columnMappings = GetColumnMappings(formatType);

				// build create table sql statement - always add a field named _Id that is the primary key
				string sql = "create table " + name + "( _Id int identity(1,1) primary key, ";

				foreach (var cm in columnMappings)
				{
					if (cm.ToColumnName != "")
					{
						sql += cm.ToColumnName + " " + cm.ToDataType + ", ";
					}
				}

				sql = sql + ")";

				// execute create table statement via sql client 
				SqlCreateTable(sql);
			}
			catch 
			{
				return "FAILURE";
			}

			return "SUCCESS"; 
		}

		private string SetupTables(WebBcpUploadModel vm)
		{
			string ret = "SUCCESS";

			switch (vm.Option)
			{
				case "ONE_TABLE":

					#region all records into one table

					if (SqlTableExists(vm.SingleTableName))
					{
						// table already exists, full stop
						ret = "TABLE_EXISTS";

						foreach (var f in vm.Files)
						{
							vm.Log.Add(
								new BcpLog()
								{
									FileName = f,
									TableName = vm.SingleTableName,
									TableCreation = ret
								}
							);
						}
					}
					else
					{
						// create table
						var tableCreation = BuildTable(vm.FormatType, vm.SingleTableName);

						foreach (var f in vm.Files)
						{	 
							vm.Log.Add(
								new BcpLog()
								{
									FileName = f,
									TableName = vm.SingleTableName,
									TableCreation = tableCreation
								}
							);
						}
					}

					#endregion

					break;

				case "INDIVIDUAL_TABLES":

					#region each file into its own table

					foreach (var f in vm.Files)
					{
						string tableName = vm.FormatType + "_@_" + Path.GetFileNameWithoutExtension(f);

						// clean up the name
						tableName = 
							tableName.Replace(" ", "_")
								.Replace("-", "_")
								.Replace("(", "_") 
								.Replace(")", "_") 
								.Replace("[", "_") 
								.Replace("]", "_"); 

						if (SqlTableExists(tableName))
						{
							// table already exists, full stop
							ret = "TABLE_EXISTS";

							vm.Log.Add(
								new BcpLog()
								{
									FileName = f,
									TableName = tableName,
									TableCreation = "TABLE_EXISTS"
								}
							);

						}
						else
						{
							// create table
							string tableCreation = BuildTable(vm.FormatType, tableName);

							vm.Log.Add(
								new BcpLog()
								{
									FileName = f,
									TableName = tableName,
									TableCreation = tableCreation
								}
							);

						}
					}

					#endregion

					break;

				default:
					throw new NotImplementedException();
					break;
			}

			return ret;
		}

		#endregion

		// GET api/<controller>/Get
		public List<NumericIdDupleType> Get()
		{
			var formatTypes =
				GetFormatTypes()
					.Select(i =>
						new NumericIdDupleType
						{
							Id = i.Id,
							Value = i.Name
						}
					)
					.ToList<NumericIdDupleType>();

			return formatTypes;
		}

		// GET api/<controller>/Get/5
		public string Get(int id)
		{
			return "";
		}

		// POST api/<controller>/Post  -> note the [FromBody] parameter attribute, if you miss adding it you will get endless 404s 
		public async Task<WebBcpUploadModel> Post([FromBody] WebBcpUploadModel vm)
		{
			// setup log 
			vm.Log = new List<BcpLog>();

			// setup table(s) according to options
			string status = SetupTables(vm);

			var columnMappings = GetColumnMappings(vm.FormatType);

			if (status == "SUCCESS")
			{
				// get all successfully setup tables
				var FilesToProcess =
					vm.Log
						.Where(i => i.TableCreation == "SUCCESS")
						.ToArray();

				// setup array to allow bcp to proceed in parallel
				Task<int>[] tasks = new Task<int>[FilesToProcess.Count()];

				for (var loop = 0; loop < FilesToProcess.Count(); loop++)
				{
					tasks[loop] = BcpFile(vm.FormatType, FilesToProcess[loop].FileName, FilesToProcess[loop].TableName, columnMappings);
				}

				int[] results = await Task.WhenAll(tasks);

				// gather the results and update the log
				for (var loop = 0; loop < FilesToProcess.Count(); loop++)
				{
					// get log record
					var log =
						vm.Log
							.Where(i => i.TableName == FilesToProcess[loop].TableName && i.FileName == FilesToProcess[loop].FileName)
							.First();

					// update the count
					log.ImportCount = tasks[loop].Result;
				}
			}

			var jsonModel = JsonConvert.SerializeObject(vm);

			return vm;
		}

		// PUT api/<controller>/Put/5
		public void Put(int id, [FromBody]string value)
		{
		}

		// DELETE api/<controller>/Delete/5
		public void Delete(int id)
		{
		}
	}
}