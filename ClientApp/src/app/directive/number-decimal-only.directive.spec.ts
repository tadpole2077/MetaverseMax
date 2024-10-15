import { Component, DebugElement } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { NumberDecimalDirective } from './number-decimal-only.directive';

@Component({
    standalone: true,
    template: ` <h2>Number Decimal Only</h2>
              <input #numberTest [numbersDecimalOnly] type='text' value='1.1' />`,
    imports: [NumberDecimalDirective],
})
class TestNumberDecimalComponent {}

describe('NumberDecimalDirective', () => {
    let fixture: ComponentFixture<TestNumberDecimalComponent>;
    let des: DebugElement[]; // only one element w/ the directive

    beforeEach(() => {
        fixture = TestBed.createComponent(TestNumberDecimalComponent);
        //fixture = TestBed.configureTestingModule({
        //  imports: [NumberDecimalDirective, TestNumberDecimalComponent],
        //}).createComponent(TestNumberDecimalComponent);

        //let component = fixture.componentInstance;

        fixture.detectChanges(); // initial binding

        // all elements with an attached NumberDecimalDirective
        des = fixture.debugElement.queryAll(By.directive(NumberDecimalDirective));
        //des = fixture.nativeElement.querySelector('input');


    // the h2 without the HighlightDirective
    //bareH2 = fixture.debugElement.query(By.css('h2:not([highlight])'));
    });

    it('should have value 1.1', () => {
    // easier to work with nativeElement
        const input = des[0].nativeElement as HTMLInputElement;
        expect(input.value).toBe('1.1');
    });

    it('change imput value to 1.1a, should have 1.1', () => {
    // easier to work with nativeElement
        const input = des[0].nativeElement as HTMLInputElement;

        input.value = '1.1a';

        // Dispatch a DOM event so that Angular responds to the input value change.
        input.dispatchEvent(new Event('input'));

        expect(input.value).toBe('1.1');
    });

    it('change imput value to a1a.a1a, should have 1.1', () => {
    // easier to work with nativeElement
        const input = des[0].nativeElement as HTMLInputElement;

        input.value = 'a1a.a1a';

        // Dispatch a DOM event so that Angular responds to the input value change.
        input.dispatchEvent(new Event('input'));

        expect(input.value).toBe('1.1');
    });

    it('change imput value to aaa.a1a0, should have 0.1', () => {

        const input = des[0].nativeElement as HTMLInputElement;

        input.value = 'aaa.a1a0';

        // Dispatch a DOM event so that Angular responds to the input value change.
        input.dispatchEvent(new Event('input'));

        expect(input.value).toBe('.10');
    });

    it('change imput value to aaa.a1a.20, should have 0.12', () => {

        const input = des[0].nativeElement as HTMLInputElement;

        input.value = 'aaa.a1a.20';

        // Dispatch a DOM event so that Angular responds to the input value change.
        input.dispatchEvent(new Event('input'));

        expect(input.value).toBe('NaN');
    });

    it('Enforce 4 decimal places, change input value to 1.202234, should have 1.2022', () => {
    // easier to work with nativeElement
        const input = des[0].nativeElement as HTMLInputElement;

        input.value = '1.202234';

        // Dispatch a DOM event so that Angular responds to the input value change.
        input.dispatchEvent(new Event('input'));

        expect(input.value).toBe('1.2022');
    });

    it('fraction only, change input value to 0.002, should have 0.002', () => {

        const input = des[0].nativeElement as HTMLInputElement;

        input.value = '0.002';

        // Dispatch a DOM event so that Angular responds to the input value change.
        input.dispatchEvent(new Event('input'));

        expect(input.value).toBe('0.002');
    });

    // Interesting test case of checking for preventDefault triggered by keypress of period char.
    it('keydown should preventDefault when char(.) is pressed and input already contains a char(.)', () => {

        const input = des[0].nativeElement as HTMLInputElement;
        input.value = '1.2';

        // Create period keypress test event
        const event = new KeyboardEvent('keydown', { bubbles: true, key: '.' });

        // Use spyOn to check if preventDefault was triggered.
        const preventDefaultSpy = spyOn(event, 'preventDefault').and.stub();
        input.dispatchEvent(event);

        expect(preventDefaultSpy).toHaveBeenCalled();
    });
});


/*
  // color tests
  it('should have three highlighted elements', () => {
    expect(des.length).toBe(3);
  });

  it('should color 1st <h2> background "yellow"', () => {
    const bgColor = des[0].nativeElement.style.backgroundColor;
    expect(bgColor).toBe('yellow');
  });

  it('should color 2nd <h2> background w/ default color', () => {
    const dir = des[1].injector.get(HighlightDirective) as HighlightDirective;
    const bgColor = des[1].nativeElement.style.backgroundColor;
    expect(bgColor).toBe(dir.defaultColor);
  });

  it('should bind <input> background to value color', () => {
    // easier to work with nativeElement
    const input = des[2].nativeElement as HTMLInputElement;
    expect(input.style.backgroundColor).withContext('initial backgroundColor').toBe('cyan');

    input.value = 'green';

    // Dispatch a DOM event so that Angular responds to the input value change.
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(input.style.backgroundColor).withContext('changed backgroundColor').toBe('green');
  });

  it('bare <h2> should not have a customProperty', () => {
    expect(bareH2.properties['customProperty']).toBeUndefined();
  });

  // Removed on 12/02/2016 when ceased public discussion of the `Renderer`. Revive in future?
  // // customProperty tests
  // it('all highlighted elements should have a true customProperty', () => {
  //   const allTrue = des.map(de => !!de.properties['customProperty']).every(v => v === true);
  //   expect(allTrue).toBe(true);
  // });

  // injected directive
  // attached HighlightDirective can be injected
  it('can inject `HighlightDirective` in 1st <h2>', () => {
    const dir = des[0].injector.get(HighlightDirective);
    expect(dir).toBeTruthy();
  });

  it('cannot inject `HighlightDirective` in 3rd <h2>', () => {
    const dir = bareH2.injector.get(HighlightDirective, null);
    expect(dir).toBe(null);
  });

  // DebugElement.providerTokens
  // attached HighlightDirective should be listed in the providerTokens
  it('should have `HighlightDirective` in 1st <h2> providerTokens', () => {
    expect(des[0].providerTokens).toContain(HighlightDirective);
  });

  it('should not have `HighlightDirective` in 3rd <h2> providerTokens', () => {
    expect(bareH2.providerTokens).not.toContain(HighlightDirective);
  });*/

