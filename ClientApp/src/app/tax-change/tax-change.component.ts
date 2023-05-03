import { Component, Inject, ViewChild, Output, EventEmitter, AfterViewInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { TaxChange } from '../district-summary/data-district-interface';
import { Globals, WORLD } from '../common/global-var';


@Component({
  selector: 'app-tax-change',
  templateUrl: './tax-change.component.html',
  styleUrls: ['./tax-change.component.css']
})
export class TaxChangeComponent implements AfterViewInit {

  public taxChangeList: TaxChange[];
  public hidePaginator: boolean;
  public hidePaginatorSold: boolean;

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);
  dataSourceSold = new MatTableDataSource(null);

  @ViewChild("sortTaxChange", { static: true }) sortTaxChange: MatSort;
  @ViewChild("paginatorTax", { static: false }) paginatorTax: MatPaginator;


  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['change_date', 'tax_type', 'tax', 'change_desc', 'change_owner'];

  constructor(public globals: Globals, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {//, cdr: ChangeDetectorRef) {

    this.httpClient = http;
    this.baseUrl = baseUrl + "api/" + globals.worldCode;

    this.taxChangeList = new Array;

  }

  // AfterViewInit called after the View has been rendered, hook to this method via the implements class hook
  ngAfterViewInit() {
    
  }

  public getTaxChange(districtId: number) {

    let params = new HttpParams();
    params = params.append('district_id', districtId.toString());

    this.httpClient.get<TaxChange[]>(this.baseUrl + '/district/gettaxchange', { params: params })
      .subscribe((result: TaxChange[]) => {

        this.taxChangeList = result;

        if (this.taxChangeList.length > 0) {

          this.dataSource = new MatTableDataSource<TaxChange>(this.taxChangeList);
          this.hidePaginator = this.taxChangeList.length == 0 || this.taxChangeList.length < 5 ? true : false;

          this.dataSource.paginator = this.paginatorTax;
          if (this.dataSource.paginator) {
            this.dataSource.paginator.firstPage();
          }

          this.dataSource.sort = this.sortTaxChange;

        }
        else {
          this.dataSource = new MatTableDataSource<TaxChange>(null);
        }

        this.dataSource.sortingDataAccessor = (item: TaxChange, property) => {
          switch (property) {
            case 'change_date': return new Date(item.change_date);
            default: return item[property];
          }
        };

      }, error => console.error(error));

    return;
  }

  sortData(sort: MatSort) {

    this.taxChangeList = this.taxChangeList.sort((a, b) => {
      const isAsc = sort.direction === 'asc';

      switch (sort.active) {
        case 'change_date': return compareDate(a.change_date, b.change_date, isAsc);
        default: return 0;
      }    
    });
  }
 
}

function compareDate(a: string, b: string, isAsc: boolean) {

  return (convertDate(a) < convertDate(b) ? -1 : 1) * (isAsc ? 1 : -1);
}

function compare(a: number | string, b: number | string, isAsc: boolean) {
  return (a < b ? -1 : 1) * (isAsc ? 1 : -1);
}

function convertDate(sourceDate: string) {

  return sourceDate.replace("/Jan/", "01")
    .replace("/Feb/", "02")
    .replace("/Mar/", "03")
    .replace("/Apr/", "04")
    .replace("/May/", "05")
    .replace("/Jun/", "06")
    .replace("/Jul/", "07")
    .replace("/Aug/", "08")
    .replace("/Sep/", "09")
    .replace("/Oct/", "10")
    .replace("/Nov/", "11")
    .replace("/Dec/", "12");
}
