import { ChangeDetectionStrategy, Component, model } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-spot-search-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatFormFieldModule, MatInputModule],
  template: `
    <mat-form-field appearance="outline" subscriptSizing="dynamic" class="spot-search">
      <mat-label>Buscar</mat-label>
      <input
        matInput
        type="search"
        autocomplete="off"
        placeholder="Parqueadero, placa, visitante o apto/casa"
        [value]="term()"
        (input)="term.set($any($event.target).value)"
      />
    </mat-form-field>
  `,
  styles: `
    .spot-search {
      width: 100%;
      max-width: 28rem;
    }
  `,
})
export class SpotSearchField {
  readonly term = model('');
}
