import { Component, OnInit, Output, EventEmitter, Inject, ViewChild, Input } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Observable } from 'rxjs';
import { map, startWith } from 'rxjs/operators';
import { MatAutocompleteSelectedEvent, MatAutocompleteTrigger } from '@angular/material/autocomplete';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Application, WORLD } from '../common/global-var';

interface OwnerName {
  public_key: string;
  name: string;
  avatar_id: number;
}

// Using a complex autocomplete-input component, meaning using collection of objects(OwnerName) and not simple text per option.  Reason: need access to matching player key and avator img on selection of a (option)name
// This usage is not native/standard, meaning the input.setValue() method accepts a string not an object. But it is assigned an object from autocomplete component (lower level code) on manual selection of an option.
// As a result, needs to handle both string (playerName) and object (player object) within the solution.  For example the [displayWith]="displayFn" method directive, handles both string assigned and object assigned.
//
@Component({
    selector: 'app-player-menu',
    templateUrl: './player-menu.component.html',
    styleUrls: ['./player-menu.component.css'],
})
export class PlayerMenuComponent implements OnInit {

    playerNameControl = new FormControl('');
    filteredOptions: Observable<OwnerName[]>;

    httpClient: HttpClient;
    baseUrl: string;
    ownerNameList: OwnerName[] = null;
    //private mobileView: boolean = false;
    public selected_player_avatar_id: number;
    public citizenURL: string;

  @ViewChild(MatAutocompleteTrigger, { static: true }) _auto: MatAutocompleteTrigger;
  @Output() selectPlayerEvent = new EventEmitter<any>();

  constructor(public globals: Application, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

      this.httpClient = http;
      this.baseUrl = baseUrl + 'api/' + globals.worldCode;

      this.loadPlayerMenuDropDown();

      this.selected_player_avatar_id = -1;

  }

  ngOnInit() {

      // On Change of Input - each element in source list is checked. valueChanges method is an Observable event.
      // Within Html using mat-option with "| sync", allows use of pipe().  This avoid need to subscribe() and unsubscribe()
      // Added extra check (typeof == object), due to when a autocomplete is selected, the option returned is full object and not a 'user typed string part' - in this case return the null list.
      this.filteredOptions = this.playerNameControl.valueChanges.pipe(
          startWith(''),
          map(ownerName => (
              ownerName ? (typeof ownerName == 'object' ? [] : this._filter(ownerName))
                  : (this.ownerNameList ? this.ownerNameList.slice() : [])))
      );

  }

  private _filter(value: string): OwnerName[] {

      // Special case - on list selection, the object returned will be the full source element (in this case OwnerName obj) and not a search string.
      const filterValue = value.toLowerCase();
      let filterOptions: OwnerName[];

      if (this.ownerNameList != null) {

          filterOptions = this.ownerNameList
              .filter(option => option.name.toLowerCase().includes(filterValue));
          //.map(ownerName => ownerName.name);
      }
      else {
          filterOptions = [];
      }

      return filterOptions;
  }

  loadPlayerMenuDropDown() {

      const params = new HttpParams();
      //params = params.append('owner_matic_key', maticKey);

      this.httpClient.get<OwnerName[]>(this.baseUrl + '/ownerdata/GetOwnerWithName', { params: params })
          .subscribe({
              next: (result) => {

                  this.ownerNameList = result;

              },
              error: (error) => { console.error(error); }
          });

      return;
  }

  updateWalletKey(selected: MatAutocompleteSelectedEvent) {

      const selectPlayer = selected.option.value;
      console.log(selectPlayer.public_key);

      // Update selected avator image
      this.selected_player_avatar_id = selectPlayer.avatar_id;
      this.citizenURL = this.globals.worldURLPath + 'citizen/' + this.selected_player_avatar_id;    

      this.selectPlayerEvent.emit(selectPlayer.public_key);

      return;
  }

  // Autocomplete > Function that maps an option's control value (which may not be an object) to its display value (string type) in the trigger.
  displayFn(option: any) {

      return option === '' || option === undefined ? '' :
          typeof option == 'object' ? option.name : option;
  }

  clearPlayerName() {

      this.playerNameControl.setValue('');

      this.selected_player_avatar_id = -1;
      this.citizenURL = '';

      this.selectPlayerEvent.emit();
  }

  // Wallet key Keypress >> Find matching name in ownerList to entered key, assign name to PlayerName input
  matchKey(playerKey:string) {

      if (playerKey) {

          // Find matching Name object (name, wallet_key, avator_id)
          const nameMatch = this.ownerNameList
              .filter(option => option.public_key.toLowerCase() == playerKey.toLowerCase());

          let currentSelectedName: string;
          if (typeof this.playerNameControl.value == 'object') {
              const selectedPlayerNameObj: OwnerName = this.playerNameControl.value;
              currentSelectedName = selectedPlayerNameObj.name;
          }
          else {
              currentSelectedName = this.playerNameControl.value;
          }

          if (nameMatch && nameMatch.length > 0 && (
              (nameMatch[0].name != currentSelectedName))) {

              if (typeof nameMatch == 'object') {
                  this.selected_player_avatar_id = nameMatch[0].avatar_id;
                  this.citizenURL = this.globals.worldURLPath + 'citizen/' + this.selected_player_avatar_id;
              }
        
              this.playerNameControl.setValue(nameMatch[0].name);
        
          }
          else if (currentSelectedName != '' && nameMatch && nameMatch.length == 0) {
              this.playerNameControl.setValue('');
              this.selected_player_avatar_id = -1;
              this.citizenURL = '';
          }

      }

  }
}
