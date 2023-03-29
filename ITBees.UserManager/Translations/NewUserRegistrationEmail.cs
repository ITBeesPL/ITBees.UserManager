using ITBees.Translations.Interfaces;

namespace ITBees.UserManager.Translations
{
    public class NewUserRegistrationEmail : ITranslate
    {
        public static readonly string ComposeEmailConfirmationSubject = "Welcome to platform [[PLATFORM_NAME]]";
        public static readonly string ComposeEmailConfirmationBody = @"
<!DOCTYPE html>
<html>
<head>
	<meta charset=""UTF-8"">
	<title>Welcome to our platform [[PLATFORM_NAME]]!</title>
</head>
<body>
	<header>
		<h1>Welcome to our platform [[PLATFORM_NAME]]!</h1>
	</header>
	<main>
		<p>We're glad you've joined us! Our website offers many possibilities to help you easily find what you're looking for.</p>
		<p>Please activate your account using the link: [[EMAIL_CONFIRMATION_URL]]/[[CONFIRMATION_PARAMETERS]]</p>
	</main>
	<footer>
		<p>We wish you a pleasant experience on our website!</p>
	</footer>
</body>
</html>


";
    }
}