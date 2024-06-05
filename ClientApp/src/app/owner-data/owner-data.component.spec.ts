import { Dialog } from '@angular/cdk/dialog';
import { HttpClient, HttpHandler } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatBottomSheet, MatBottomSheetModule } from '@angular/material/bottom-sheet';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Application } from '../common/global-var';

import { OwnerDataComponent } from './owner-data.component';

describe('OwnerDataComponent', () => {
  let component: OwnerDataComponent;
  let fixture: ComponentFixture<OwnerDataComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ OwnerDataComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {

    fixture = TestBed
      .configureTestingModule({
        imports: [MatBottomSheetModule, //RouterModule,
          //{
       //     import: ActivatedRoute, useClass: new ActivatedRoute()
       //   }
        ],
        providers: [Application, HttpClient, HttpHandler//,
          //{
          //    provide: ActivatedRoute, useClass: null
          //}
        ],
        declarations: []
      })
      .createComponent(OwnerDataComponent);

    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
