/***************************************************************************************************
 * Load `$localize` onto the global scope - used if i18n tags appear in Angular templates.
 */
import '@angular/localize/init';
import { enableProdMode } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { AppModule } from './app/app.module';
import { environment } from './environments/environment';

export function getBaseUrl() {
    return document.getElementsByTagName('base')[0].href;
}

const providers = [
    { provide: 'BASE_URL', useFactory: getBaseUrl, deps: [] }
];

if (environment.production) {
    enableProdMode();
}

// modules flow - non standalone, can add standalone component as additional.
platformBrowserDynamic(providers).bootstrapModule(AppModule, {
    //providers: [],
})
    .catch(err => console.log(err));
