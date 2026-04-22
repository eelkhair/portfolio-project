/*
 * Injects a "Create account" link and a "Continue as Guest" action on the Keycloak
 * Sign In page.
 *
 * "Create account" — points to the originating app's /signup route, derived from the
 *   redirect_uri query parameter that Keycloak receives from the OIDC client.
 *
 * "Continue as Guest" — calls a backend endpoint that provisions a throwaway Keycloak
 *   user (tagged anonymous=true) and returns {username, password}. The JS types those
 *   into #kc-form-login and submits, so Keycloak authenticates the guest via the normal
 *   login flow — no token handling on the frontend, no OIDC lib surprises.
 *
 * The same theme serves both the angular-public and angular-admin clients; we branch
 * on the client_id query param to pick the right endpoint (and thus the right group
 * assignment — /Applicants vs /Admins).
 */
(function () {
  var PUBLIC_CLIENT_ID = 'angular-public';
  var ADMIN_CLIENT_ID = 'angular-admin';

  function getRedirectUrl() {
    try {
      var params = new URLSearchParams(window.location.search);
      var redirectUri = params.get('redirect_uri');
      return redirectUri ? new URL(redirectUri) : null;
    } catch (e) {
      return null;
    }
  }

  function getClientId() {
    try {
      return new URLSearchParams(window.location.search).get('client_id');
    } catch (e) {
      return null;
    }
  }

  function getSignupUrl() {
    var u = getRedirectUrl();
    return u ? u.origin + '/signup' : null;
  }

  // Maps the frontend origin (from redirect_uri) to the gateway API origin.
  //   localhost:*         → http://localhost:5280         (Aspire monolith direct)
  //   jobs.<d>            → job-gateway.<d>               (prod)
  //   job-admin.<d>       → job-gateway.<d>               (prod)
  //   jobs-dev.<d>        → job-gateway-dev.<d>           (dev — preserve -dev suffix)
  //   job-admin-dev.<d>   → job-gateway-dev.<d>           (dev — preserve -dev suffix)
  function getApiBaseUrl() {
    var u = getRedirectUrl();
    if (!u) return null;

    if (u.hostname === 'localhost' || u.hostname === '127.0.0.1') {
      return 'http://localhost:5280';
    }

    var parts = u.hostname.split('.');
    if (parts.length < 2) return null;
    var leftmost = parts[0];
    var baseDomain = parts.slice(1).join('.');
    var gatewayLabel = /-dev$/i.test(leftmost) ? 'job-gateway-dev' : 'job-gateway';
    return u.protocol + '//' + gatewayLabel + '.' + baseDomain;
  }

  function getAnonymousEndpoint() {
    var clientId = getClientId();
    var api = getApiBaseUrl();
    if (!api) return null;
    if (clientId === PUBLIC_CLIENT_ID) return api + '/api/Account/signup/public/anonymous';
    if (clientId === ADMIN_CLIENT_ID) return api + '/api/Account/signup/admin/anonymous';
    return null;
  }

  function showError(container, message) {
    var existing = container.querySelector('.jobboard-guest-error');
    if (existing) existing.remove();
    var err = document.createElement('div');
    err.className = 'jobboard-guest-error';
    err.textContent = message;
    err.style.cssText =
      'margin-top:0.5rem;color:#b91c1c;font-size:0.85rem;';
    container.appendChild(err);
  }

  function handleGuestClick(button, container) {
    return function (ev) {
      ev.preventDefault();
      var endpoint = getAnonymousEndpoint();
      if (!endpoint) {
        showError(container, 'Guest login is not available for this app.');
        return;
      }

      button.disabled = true;
      var originalLabel = button.textContent;
      button.textContent = 'Creating guest session...';

      fetch(endpoint, {
        method: 'POST',
        credentials: 'omit',
        headers: {
          'Accept': 'application/json',
          // Gateway's YARP catch-all route for /api/** requires x-mode to pick a
          // cluster. Signup lives on the monolith regardless of admin/public, so
          // pin it to monolith. Without this the gateway returns 404.
          'x-mode': 'monolith'
        }
      })
        .then(function (res) {
          if (res.status === 429) {
            throw new Error(
              'Too many guest sessions from your network. Try again in an hour, or sign in above.'
            );
          }
          if (!res.ok) {
            throw new Error('Guest session could not be created. Please try again.');
          }
          return res.json();
        })
        .then(function (envelope) {
          // Accept either { data: { username, password } } (ApiResponse envelope) or
          // { username, password } directly.
          var payload = (envelope && envelope.data) ? envelope.data : envelope;
          if (!payload || !payload.username || !payload.password) {
            throw new Error('Guest session response was malformed.');
          }

          var form = document.querySelector('form#kc-form-login');
          var userInput = document.getElementById('username');
          var passInput = document.getElementById('password');
          if (!form || !userInput || !passInput) {
            throw new Error('Login form not found.');
          }

          userInput.value = payload.username;
          passInput.value = payload.password;
          form.submit();
        })
        .catch(function (err) {
          button.disabled = false;
          button.textContent = originalLabel;
          showError(container, err && err.message ? err.message : 'Something went wrong.');
        });
    };
  }

  function inject() {
    // Only render on the Sign In page. Other Keycloak pages (error, info, etc.)
    // typically don't have #kc-form-login.
    var form = document.querySelector('form#kc-form-login');
    if (!form) return;

    // Avoid duplicate insertion if script somehow runs twice.
    if (document.querySelector('#jobboard-signup-link')) return;

    var signupUrl = getSignupUrl();
    var clientId = getClientId();
    var canGuest = (clientId === PUBLIC_CLIENT_ID || clientId === ADMIN_CLIENT_ID) && getApiBaseUrl();

    var container = document.createElement('div');
    container.id = 'jobboard-signup-link';
    container.style.cssText =
      'margin-top:1.25rem;text-align:center;font-size:0.9rem;';

    if (signupUrl) {
      var label = document.createTextNode("Don't have an account? ");
      var link = document.createElement('a');
      link.href = signupUrl;
      link.textContent = 'Create account';
      link.style.cssText = 'font-weight:500;';
      container.appendChild(label);
      container.appendChild(link);
    }

    if (canGuest) {
      if (signupUrl) {
        var divider = document.createElement('div');
        divider.textContent = 'or';
        divider.className = 'jobboard-guest-divider';
        divider.style.cssText =
          'margin:0.75rem 0 0.5rem;color:#6b7280;font-size:0.8rem;letter-spacing:0.05em;text-transform:uppercase;';
        container.appendChild(divider);
      }

      var guestBtn = document.createElement('button');
      guestBtn.type = 'button';
      guestBtn.id = 'jobboard-guest-button';
      guestBtn.className = 'jobboard-guest-button';
      guestBtn.textContent = 'Continue as Guest';
      guestBtn.addEventListener('click', handleGuestClick(guestBtn, container));
      container.appendChild(guestBtn);

      var hint = document.createElement('div');
      hint.textContent = 'Instant access with a throwaway account.';
      hint.style.cssText = 'margin-top:0.4rem;color:#6b7280;font-size:0.75rem;';
      container.appendChild(hint);
    }

    if (container.childNodes.length === 0) return;

    // Insert after the form so it reads as a natural footer.
    form.parentNode.insertBefore(container, form.nextSibling);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', inject);
  } else {
    inject();
  }
})();
