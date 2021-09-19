using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;


namespace Locking
{
	public class Dependency : IDisposable
	{
		private readonly TelemetryClient _telemetry;
		private readonly IOperationHolder<DependencyTelemetry> _operation;

		public Dependency(TelemetryClient telemetry, string objectType, string operation, string id = "")
		{
			_telemetry = telemetry;
			var dependency = new DependencyTelemetry
			{
				Target = objectType,
				Name = operation,
				Data = id,
				Success = true
			};
			_operation = _telemetry.StartOperation(dependency);
		}

		public bool? Success { get { return _operation.Telemetry.Success; } set { _operation.Telemetry.Success = value; } }
		public void Dispose()
		{
			_telemetry.StopOperation(_operation);
			GC.SuppressFinalize(this);
		}
	}
}
