import { Component, HostBinding, NgZone } from '@angular/core';
import { OverlayContainer } from '@angular/cdk/overlay';
import { Application, WORLD } from './common/global-var';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent {
  title = 'app';
  @HostBinding('class') className = '';

  constructor(public globals: Application, private overlay: OverlayContainer, private zone: NgZone) {
    globals.appComponentInstance = this;
  }

  darkModeChange(darkModeEnabled: boolean) {

    let x:number = 1;
    
    const darkClassName = "darkMode";
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
