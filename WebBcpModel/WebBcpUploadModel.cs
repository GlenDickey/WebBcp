using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebBcpModel
{
	public class NumericIdDupleType
	{
		public int Id { get; set; }
		public string Value { get; set; }
	}

	public class BcpLog
	{
		public string TableName { get; set; }
		public string FileName { get; set; }		
		public string TableCreation { get; set; }
		public int ImportCount { get; set; }
	}

	public class WebBcpUploadModel
	{
		public WebBcpUploadModel() { }

		public string FormatType { get; set; }
		public string Option { get; set; }
		public string SingleTableName { get; set; }
		public List<string> Files { get; set; }
		public List<BcpLog> Log { get; set; }
	}
}
