import { Component, DebugElement } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { NumberDirective } from './numberonly.directive';

@Component({
    //standalone: true,
    template: ` <h2>Number Only</h2>
              <input #numberTest [numbersOnly] type='text' value='1' />`,
    //imports: [NumberDirective],
})
class TestNumberOnlyComponent {}

describe('NumberDirective', () => {
    let component: TestNumberOnlyComponent;
    let fixture: ComponentFixture<TestNumberOnlyComponent>;
    let des: DebugElement[]; // only one elements w/ the directive

    beforeEach(() => {

        fixture = TestBed.configureTestingModule({
            declarations: [NumberDirective,TestNumberOnlyComponent],
        }).createComponent(TestNumberOnlyComponent);


        // all elements with an attached NumberDecimalDirective
        des = fixture.debugElement.queryAll(By.directive(NumberDirective));

    // the h2 without the HighlightDirective
    //bareH2 = fixture.debugElement.query(By.css('h2:not([highlight])'));
    });

    it('should have value 1', () => {
    // easier to work with nativeElement
        const input = des[0].nativeElement as HTMLInputElement;
        expect(input.value).toBe('1');
    });

    it('change imput value to 1.1a, should have 11', () => {
    // easier to work with nativeElement
        const input = des[0].nativeElement as HTMLInputElement;

        input.value = '1.1a';

        // Dispatch a DOM event so that Angular responds to the input value change.
        input.dispatchEvent(new Event('input'));

        expect(input.value).toBe('11');
    });

});
