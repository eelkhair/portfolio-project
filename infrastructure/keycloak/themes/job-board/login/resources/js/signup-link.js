/*
 * Injects a "Create account" link on the Keycloak Sign In page.
 *
 * The signup URL is derived from the redirect_uri query parameter that Keycloak
 * receives when a client (angular-admin or angular-public) initiates OIDC auth.
 * One script works across every client + realm combination without per-environment
 * configuration: whatever origin the user came from is the origin they'll be sent
 * back to for /signup.
 */
(function () {
  function getSignupUrl() {
    try {
      var params = new URLSearchParams(window.location.search);
      var redirectUri = params.get('redirect_uri');
      if (!redirectUri) return null;
      var u = new URL(redirectUri);
      return u.origin + '/signup';
    } catch (e) {
      return null;
    }
  }

  function inject() {
    // Only render on the Sign In page. Other Keycloak pages (error, info, etc.)
    // typically don't have #kc-form-login.
    var form = document.querySelector('form#kc-form-login');
    if (!form) return;

    // Avoid duplicate insertion if script somehow runs twice.
    if (document.querySelector('#jobboard-signup-link')) return;

    var signupUrl = getSignupUrl();
    if (!signupUrl) return;

    var container = document.createElement('div');
    container.id = 'jobboard-signup-link';
    container.style.cssText =
      'margin-top:1.25rem;text-align:center;font-size:0.9rem;';

    var label = document.createTextNode("Don't have an account? ");
    var link = document.createElement('a');
    link.href = signupUrl;
    link.textContent = 'Create account';
    link.style.cssText = 'font-weight:500;';

    container.appendChild(label);
    container.appendChild(link);

    // Insert after the form so it reads as a natural footer.
    form.parentNode.insertBefore(container, form.nextSibling);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', inject);
  } else {
    inject();
  }
})();
