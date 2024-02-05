using ITBees.Translations.Interfaces;

namespace ITBees.UserManager.Translations
{
    public class NewUserRegistrationEmail : ITranslate
    {
        public static readonly string ComposeEmailWithInvitationToOrganizationSubject = "[[INVITING_NAME]] has invited You to company : [[COMPANY_NAME]]";
        public static readonly string ComposeEmailWithInvitationToOrganizationBody = @"
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
		<p>[[INVITING_NAME]] invited you to join the organization [[COMPANY_NAME]]</p>
		<p>Please if You accept this invitation click link : <a href=""[[EMAIL_CONFIRMATION_URL]][[CONFIRMATION_PARAMETERS]]"">link</a><br>
	</main>
	<footer>
		<p>We wish you a pleasant experience on our website!</p>
	</footer>
</body>
</html>";
        public static readonly string ComposeEmailWithUserCreationAndInvitationToOrganizationSubject = "You have been invited to company : [[COMPANY_NAME]]";
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
		<p>Please activate your account using the <a href=""[[EMAIL_CONFIRMATION_URL]][[CONFIRMATION_PARAMETERS]]"">link</a><br>Or copy pase in webbrowser :<br> [[EMAIL_CONFIRMATION_URL]][[CONFIRMATION_PARAMETERS]]</p>
	</main>
	<footer>
		<p>We wish you a pleasant experience on our website!</p>
	</footer>
</body>
</html>


";

        public static readonly string ComposeEmailWithUserCreationAndInvitationToOrganizationBody = @"
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
		<p>[[INVITING_NAME]] invited you to join the organization [[COMPANY_NAME]]</p>
		<p>Please register your account using the <a href=""[[EMAIL_CONFIRMATION_URL]][[CONFIRMATION_PARAMETERS]]"">link</a><br>
	</main>
	<footer>
		<p>We wish you a pleasant experience on our website!</p>
	</footer>
</body>
</html>";
    }
}