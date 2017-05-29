using System;
using Dropbox.Api;

namespace PneumaticTube
{
	internal static class DropboxExceptionExtensions
	{
		public static ExitCode? HandleAuthException(this DropboxException ex)
		{
			var authException = ex as AuthException;

			if (authException == null)
			{
				return null;
			}

			Console.WriteLine($"An authentication error occurred: {authException}");

			return ExitCode.AccessDenied;
		}

		public static ExitCode? HandleAccessException(this DropboxException ex)
		{
			var accessException = ex as AccessException;

			if (accessException == null)
			{
				return null;
			}

			Console.WriteLine($"An access error occurred: {accessException}");

			return ExitCode.AccessDenied;
		}

		public static ExitCode? HandleRateLimitException(this DropboxException ex)
		{
			var rateLimitException = ex as RateLimitException;

			if (rateLimitException == null)
			{
				return null;
			}

			Console.WriteLine($"A rate limit error occurred: {rateLimitException}");

			return ExitCode.Canceled;
		}

		public static ExitCode? HandleBadInputException(this DropboxException ex)
		{
			var badInputException = ex as BadInputException;

			if (badInputException == null)
			{
				return null;
			}

			Console.WriteLine($"An error occurred: {badInputException}");

			return ExitCode.BadArguments;
		}

		public static ExitCode? HandleHttpException(this DropboxException ex)
		{
			var httpException = ex as HttpException;

			if (httpException == null)
			{
				return null;
			}

			Console.WriteLine($"An HTTP error occurred: {httpException}");

			return ExitCode.BadArguments;
		}
	}
}