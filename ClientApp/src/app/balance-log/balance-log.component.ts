import { HttpClient, HttpParams } from "@angular/common/http";
import { Component, Inject, ViewChild } from "@angular/core";
import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";
import { Application } from "../common/global-var";
import { TransactionCollection, Transaction } from '../common/interface';


@Component({
  selector: 'app-balance-log',
  styleUrls: ['./balance-log.component.css'],
  templateUrl: './balance-log.component.html',
})
export class BalanceLogComponent {

  httpClient: HttpClient;
  baseUrl: string;
  ownerTransactionLog: TransactionCollection;
  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  hidePaginator: boolean = false;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['event_recorded_gmt', 'action', 'hash', 'amount'];

  constructor(public globals: Application, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {

    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;

  }

  getOwnerLog(maticKey:string) {

    // Reset any prior table load
    //this.tableView = null;

    let params = new HttpParams();
    params = params.append('owner_matic_key', maticKey);

    this.httpClient.get<TransactionCollection>(this.baseUrl + '/transaction/getLog', { params: params })
      .subscribe({
        next: (result) => {

          this.ownerTransactionLog = result;

          if (this.ownerTransactionLog.transaction_list) {

            this.dataSource = new MatTableDataSource<Transaction>(this.ownerTransactionLog.transaction_list);
            this.dataSource.sort = this.sort;
          }
        },
        error: (error) => { console.error(error) }
      });


    return;
  }
}
