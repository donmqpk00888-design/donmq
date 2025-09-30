import { Component, OnInit } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { environment } from '@env/environment';
interface AppSettings {
  theme: string;
  panelPosition: { x: number; y: number };
}
@Component({
  selector: 'theme-switch',
  standalone: false,
  templateUrl: './theme-switch.component.html',
  styleUrls: ['./theme-switch.component.scss']
})
export class ThemeSwitchComponent implements OnInit {
  private readonly SETTINGS_KEY = 'theme-settings';
  private readonly DEFAULT_THEME = 'light';
  private readonly DEFAULT_LOCATION = { x: 7, y: -43 };
  isProduction = environment.production
  // isProduction = true

  dragPosition = { x: 7, y: -43 };
  private _setting$ = new BehaviorSubject<AppSettings>(this.getSettings());
  public setting$ = this._setting$.asObservable();

  constructor() {
    if (!this.isProduction) {
      this.dragPosition = this._setting$.value.panelPosition;
      this.applyTheme(this._setting$.value.theme);
    }
  }

  ngOnInit(): void {
  }

  isDarkMode(): boolean {
    return this._setting$.value.theme === 'dark';
  }

  onDragEnded(event: any): void {
    this.dragPosition = {
      x: event.source.getFreeDragPosition().x,
      y: event.source.getFreeDragPosition().y
    };
    this.saveSettings({ panelPosition: this.dragPosition });
  }

  toggleTheme(): void {
    const newTheme = this._setting$.value.theme === 'light' ? 'dark' : 'light';
    this.saveSettings({ theme: newTheme });
    this.applyTheme(newTheme);
  }

  private getSettings(): AppSettings {
    const saved = localStorage.getItem(this.SETTINGS_KEY);
    return saved ? JSON.parse(saved) : { theme: this.DEFAULT_THEME, panelPosition: this.DEFAULT_LOCATION };
  }

  private saveSettings(partialSettings: Partial<AppSettings>): void {
    const currentSettings = this.getSettings();
    const updatedSettings = {
      ...currentSettings, ...partialSettings,
      panelPosition: { ...currentSettings.panelPosition, ...partialSettings.panelPosition }
    };
    localStorage.setItem(this.SETTINGS_KEY, JSON.stringify(updatedSettings));
    this._setting$.next(updatedSettings);
  }

  private applyTheme(theme: string): void {
    document.body.classList.toggle('dark-mode', theme === 'dark');
  }
}
