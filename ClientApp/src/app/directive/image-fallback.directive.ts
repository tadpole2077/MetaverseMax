import {Directive, Input, HostBinding} from '@angular/core'
@Directive({
  selector: 'img[imgfallback]',
  host: {
    '(error)': 'updateUrl()',
    '[src]': 'src'
  }
})

// Used to assign a default image (via attribute default) when any image loading error occurs - typically a 404 image not found.
export class ImageFallbackDirective {
  @Input() src: string;
  @Input() default: string;

  updateUrl() {
    if (this.default) {
      this.src = this.default;
    }
  }
}
