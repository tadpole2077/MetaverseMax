import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AppComponent } from './app.component';
import { Application } from './common/global-var';
import { NavMenuComponent } from './nav-menu/nav-menu.component';

// creating a stub spy for Application - global service
// using angular component : app-nav-menu

describe('AppComponent', () => {

  // Stub Custom Components - include in declarations
  @Component({selector: 'app-nav-menu', template: ''})
    class NavMenuComponent { }

  let component: AppComponent;
  let fixture: ComponentFixture<AppComponent>;

  beforeEach(async () => {

      // Create Textbox and configure for target component, including all dependencies.
      await TestBed.configureTestingModule({
          imports: [
              RouterTestingModule
          ],
          providers: [
              { provide: Application, useValue: jasmine.createSpyObj('Application', ['appComponentInstance']) }
          ],
          declarations: [
              AppComponent, NavMenuComponent
          ]
      }).compileComponents();

      fixture = TestBed.createComponent(AppComponent);

      component = fixture.componentInstance;
  });

  

  it('should create the app', () => {    
      expect(component).toBeTruthy();
  });

  it('should have as title \'MetaverseMax\'', () => {
      expect(component.title).toEqual('MetaverseMax');
  });

  it('should change to darkmode', () => {

      component.darkModeChange(true);
      fixture.detectChanges();

      expect(component.className).toContain('darkMode');
  });
});
