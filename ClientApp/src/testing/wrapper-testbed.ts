import { TestBed, TestBedStatic, TestModuleMetadata } from '@angular/core/testing';
import { TestInterceptorService } from '../app/service/test-interceptor.service';


// Wrapper class - adds standard mock components to any WrapperTestBed usage instead of Testbed.
// Note that Training guide using Partial - but i think that is disadvantagous due to unavailable Testbed methods not overridden. 
export class WrapperTestBed extends TestBed {

    constructor(){
        super();    
    }

    static configureTestingModule(moduleDef: TestModuleMetadata): TestBed {

        const webStorageSpy = jasmine.createSpyObj('WebStorageService', ['getRemote', 'setRemote']);
        webStorageSpy.getRemote.and.returnValue(jasmine.createSpyObj('observable',['subscribe']));
        webStorageSpy.setRemote.and.returnValue(jasmine.createSpyObj('observable',['subscribe']));

        // using helper type - all type properties are optional, fill in ones that are used.
        const defaults: Partial<TestModuleMetadata> = {
            declarations: [],
            providers: [{ provide: TestInterceptorService, useValue: webStorageSpy }]
        };

        // Add to providers if it exists with at least one provider, otherwise create an array of default providers
        //moduleDef.providers = moduleDef.providers == null ?
        //  [{ provide: WebStorageService, useValue: webStorageSpy }] : 
        //  moduleDef.providers.concat({ provide: WebStorageService, useValue: webStorageSpy });  
    
        return TestBed.configureTestingModule({
            declarations: Object.assign([], moduleDef.declarations, defaults.declarations),
            providers: Object.assign([], moduleDef.providers, defaults.providers)
        });
    }
}
