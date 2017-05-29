namespace PneumaticTube
{
	internal enum ExitCode
	{
		Success = 0,
		FileNotFound = 2,
		AccessDenied = 5,
		BadArguments = 160,
		FileExists = 80,
		Canceled = 1223,
		UnknownError = int.MaxValue
	}
}