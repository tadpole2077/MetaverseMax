import { Component, HostBinding, NgZone } from '@angular/core';
import { OverlayContainer } from '@angular/cdk/overlay';
import { Application } from './common/global-var';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html'
})
export class AppComponent {

    title = 'MetaverseMax';
    @HostBinding('class') className = '';     // Data binding to component 'class' DOM attribute, if/when changed updates the DOM on 'change detection' event cycle 

    constructor(public globals: Application, private overlay: OverlayContainer, private zone: NgZone) {
        globals.appComponentInstance = this;
        this.darkModeChange(true);      // default theme - darkmode enabled.
    }

    darkModeChange(darkModeEnabled: boolean) {
    
        const darkClassName = 'darkMode';
        this.className = darkModeEnabled ? darkClassName : '';

        // apply theme change to parent and all child components.
        this.zone.run(() => {
            // Need to apply class to root body - as no material root control used.
            if (darkModeEnabled) {
                this.overlay.getContainerElement().parentElement.classList.add(darkClassName);
            } else {
                this.overlay.getContainerElement().parentElement.classList.remove(darkClassName);
            }
        });
    }
}
