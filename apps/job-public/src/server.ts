import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import { join } from 'node:path';

const browserDistFolder = join(import.meta.dirname, '../browser');

const app = express();
// Angular 21 SSR rejects unknown hostnames as SSRF protection. Without this
// allowlist, every render falls back to client-side and ships an empty
// <app-root>. Both prod + dev hosts on both zones are listed here; localhost
// is included for `ng serve` and Docker port-forward testing.
const angularApp = new AngularNodeAppEngine({
  allowedHosts: [
    'jobs.elkhair.tech',
    'jobs-dev.elkhair.tech',
    'jobs.eelkhair.net',
    'jobs-dev.eelkhair.net',
    'localhost',
  ],
});

/**
 * Example Express Rest API endpoints can be defined here.
 * Uncomment and define endpoints as necessary.
 *
 * Example:
 * ```ts
 * app.get('/api/{*splat}', (req, res) => {
 *   // Handle API request
 * });
 * ```
 */

/**
 * Serve static files from /browser
 */
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * Guard: if a request looks like a static asset (any path with a file extension)
 * and `express.static` above didn't serve it, the file is genuinely missing.
 * Return a real 404 instead of falling through to Angular SSR which would
 * render the SPA shell HTML — that triggers "MIME type 'text/html'" errors in
 * the browser when a stale chunk hash from a previous build is requested.
 */
app.use((req, res, next) => {
  if (/\.[a-z0-9]+$/i.test(req.path)) {
    res.status(404).type('text/plain').send('Not Found');
    return;
  }
  next();
});

/**
 * Handle all other (route-shaped) requests by rendering the Angular application.
 */
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then((response) => (response ? writeResponseToNodeResponse(response, res) : next()))
    .catch(next);
});

/**
 * Start the server if this module is the main entry point, or it is ran via PM2.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url) || process.env['pm_id']) {
  const port = process.env['PORT'] || 3000;
  app.listen(port, (error) => {
    if (error) {
      throw error;
    }

    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build) or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
