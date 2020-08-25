namespace Jellyfin.Plugin.SmartPlaylist.QueryEngine
{
	public class Expression
	{
		public string MemberName{ get; set; }
		public string Operator { get; set; }
		public string TargetValue { get; set; }
		public Expression(string MemberName, string Operator, string TargetValue)
		{
			this.MemberName = MemberName;
			this.Operator = Operator;
			this.TargetValue = TargetValue;
		}
	}
}
