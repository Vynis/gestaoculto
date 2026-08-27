import './global-polyfills';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import * as Sentry from '@sentry/angular';

import { AppModule } from './app/app.module';
import { environment } from './environments/environment';

Sentry.init({
  enabled: environment.sentryEnabled,
  dsn: environment.sentryDsn || undefined,
  environment: environment.sentryEnvironment,
  integrations: [Sentry.browserTracingIntegration()],
  tracesSampleRate: environment.production ? 0.1 : 0
});


platformBrowserDynamic().bootstrapModule(AppModule)
  .catch(err => console.error(err));
