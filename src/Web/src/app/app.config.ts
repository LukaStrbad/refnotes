import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, RouteReuseStrategy } from '@angular/router';

import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { authInterceptor } from '../interceptors/auth.interceptor';
import { provideTranslateService } from "@ngx-translate/core";
import { CustomRouteReuseStrategy } from './route-reuse-strategy';

// const httpLoaderFactory: (http: HttpClient) => TranslateHttpLoader = (http: HttpClient) =>
//   new TranslateHttpLoader(http, './i18n/', '.json');

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([authInterceptor])
    ),
    provideAnimationsAsync(),
    provideTranslateService({
      // loader: {
      //   provide: TranslateLoader,
      //   useFactory: httpLoaderFactory,
      //   deps: [HttpClient]
      // }
    }),
    { provide: RouteReuseStrategy, useClass: CustomRouteReuseStrategy },
    { provide: 'Window', useValue: window },
  ]
};
