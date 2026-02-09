import { TestBed } from '@angular/core/testing';
import { Blockchain } from './blockchain';

describe('Blockchain', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Blockchain],
    }).compileComponents();
  });

  it('should create the component', () => {
    const fixture = TestBed.createComponent(Blockchain);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render placeholder text', async () => {
    const fixture = TestBed.createComponent(Blockchain);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('p')?.textContent).toContain('Blockchain component works!');
  });
});
