import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideRpcClient } from '../rpc';

declare const __BLOCKCHAIN_HUB_URL__: string;

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideRpcClient({
      hubUrl: __BLOCKCHAIN_HUB_URL__,
    }),
  ],
};
