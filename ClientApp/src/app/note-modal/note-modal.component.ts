import { Component, Inject, ViewChild, Output, EventEmitter, AfterViewInit, Pipe, PipeTransform } from '@angular/core';
import { DragDrop } from '@angular/cdk/drag-drop';
import { DomSanitizer, SafeResourceUrl, SafeUrl } from '@angular/platform-browser';


//@Pipe({ name: 'safeHtml' })
@Component({
  selector: 'app-note-modal',
  templateUrl: './note-modal.component.html',
  styleUrls: ['./note-modal.component.css']
})
//export class NoteModalComponent implements PipeTransform, AfterViewInit {
export class NoteModalComponent implements PipeTransform, AfterViewInit {

  @Output() hideAdEvent = new EventEmitter<boolean>();
  public displayTextSafe: string;
  public displayDateTitle: string;
  
  //constructor(private sanitized: DomSanitizer) {
  constructor() {
    
  }

  // Paginator wont render until loaded in call to ngAfterViewInit, as its a  @ViewChild decalare
  // AfterViewInit called after the View has been rendered, hook to this method via the implements class hook
  ngAfterViewInit() {
    
  }

  public adShow(passedText: string, startDate: string, endDate: string) {
    this.displayDateTitle = startDate.substring(0, 11) + (endDate == "" ? "" : " - " + endDate.substring(0,11));
    this.displayTextSafe = this.displayDateTitle + "<br\>" + passedText;
    return;
  }

  transform(value) {
    //return this.sanitized.bypassSecurityTrustHtml(value);
  }

  setHide() {
    this.hideAdEvent.emit(true);
  }

}
