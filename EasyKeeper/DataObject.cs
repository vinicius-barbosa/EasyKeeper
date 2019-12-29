namespace EasyKeeper
{
	public class DataObject
	{
		public string RetrieveCommand { get => "select * from {0} {1} where {2}"; }
		public string CreateCommand { get => "insert into {0}({1}) values ({2})"; }
		public string UpdateCommand { get => "update {0} set {1} where {2}"; }
		public string DeleCommand { get => "delete from {0} where {1}"; }
	}
}
