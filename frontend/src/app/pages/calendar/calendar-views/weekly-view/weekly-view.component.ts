import { Component, OnInit, OnDestroy, HostBinding } from '@angular/core';
import { TimeEntry, CalendarDay, DateUtils } from '../../../../models/calendar';
import { ActivatedRoute, Params } from '@angular/router';
import { CalendarService } from '../../../../services/calendar.service';
import { Project } from '../../../../models/project';
import { Subscription } from 'rxjs';
import { ImpersonationService } from '../../../../services/impersonation.service';
import { User } from '../../../../models/user';
import * as moment from 'moment';

@Component({
	templateUrl: 'weekly-view.component.html',
	selector: 'ct-calendar-weekly-view'
})

export class CalendarWeeklyViewComponent implements OnInit, OnDestroy {
	@HostBinding('class.ct-calendar-weekly-view') addClass: boolean = true;

	calendar: CalendarDay[];
	date: string;
	daysInCalendar: number;
	firstDayOfWeek: number;
	projects: Project[] = [];
	projectIds: number[];
	projectTimeEntries: TimeEntry[] = [];
	startDay: string;
	endDay: string;
	timeEntries: TimeEntry[];

	private timeEntriesSubscription: Subscription;
	private subscriptionImpersonation: Subscription;

	constructor(private route: ActivatedRoute,
	            private calendarService: CalendarService,
	            private impersonationService: ImpersonationService) {
	}

	ngOnInit() {
		this.route.data.forEach((data: { user: User }) => {
			let user = this.impersonationService.impersonationUser || data.user;
			this.firstDayOfWeek = user.weekStart;
		});

		this.route.params.subscribe((params: Params) => {
			this.projectIds = params['projectIds'] ? params['projectIds'].split(',') : null;
			this.date = params['date'] ? DateUtils.reformatDate(params['date'], 'MM-DD-YYYY') : DateUtils.formatDateToString(new Date());
			this.setAvailablePeriod(window.innerWidth);
			this.setEmptyWeek();
			this.getTimeEntries(this.projectIds);
		});

		this.timeEntriesSubscription = this.calendarService.timeEntriesUpdated
			.subscribe(() => {
				this.getTimeEntries(this.projectIds);
			});
		this.subscriptionImpersonation = this.impersonationService.onChange.subscribe(() => {
			this.getTimeEntries(this.projectIds);
		});
	}

	getTotalTime(timeField: string): string {
		let time = 0;

		this.calendar.forEach((day: CalendarDay) => {
			time += this.calendarService.getTotalTimeForDay(day, timeField);
		});

		return this.formatTimeToString(time);
	}

	getTimeEntries(projIds?: number[]): void {
		this.calendarService.getTimeEntries(this.startDay, this.daysInCalendar)
			.subscribe((res) => {
				this.timeEntries = res;
				if (projIds) {
					this.filterByProject(projIds);
				} else {
					this.filterByProject();
				}
				this.sortTimeEntriesByDate();
			});
	}

	getWeekBeginning(date: string): string {
		return this.calendarService.getWeekBeginning(date, this.firstDayOfWeek);
	}

	filterByProject(projectIds?: number[]): void {
		if (projectIds && projectIds.length) {
			this.projectTimeEntries = [];
			this.timeEntries.forEach((timeEntry: TimeEntry) => {
				if (projectIds.map(x => +x).indexOf(timeEntry.projectId) !== -1) {
					this.projectTimeEntries.push(timeEntry);
				}
			});
		} else {
			this.projectTimeEntries = this.timeEntries;
		}
	}

	formatTimeToString(s: number): string {
		let m = Math.floor(s / 60);
		let h = Math.floor(m / 60);
		m = m - h * 60;
		return (((h > 99) ? ('' + h) : ('00' + h).slice(-2)) + ':' + ('00' + m).slice(-2));
	}

	onResize(event): void {
		let width: number = event.target.innerWidth;
		let lastPeriod: number = this.daysInCalendar;
		this.setAvailablePeriod(width);
		if (lastPeriod - this.daysInCalendar) {
			this.setEmptyWeek();
			this.getTimeEntries(this.projectIds);
		}
	}

	setAvailablePeriod(width: number): void {
		let oldNumberOfDays = this.daysInCalendar;
		if (width < 810 && oldNumberOfDays !== 1) {
			this.daysInCalendar = 1;
		}
		if (width >= 810 && width < 1300 && oldNumberOfDays !== 4) {
			this.daysInCalendar = 4;
		}
		if (width >= 1300 && oldNumberOfDays !== 7) {
			this.daysInCalendar = 7;
		}
		if (this.daysInCalendar !== 7) {
			this.startDay = this.date;
		} else {
			this.startDay = this.getWeekBeginning(this.date);
		}
		this.endDay = this.moveDate(moment(this.startDay).format('YYYY-MM-DD'), this.daysInCalendar - 1);
	}

	setEmptyWeek(): void {
		this.calendar = [];
		let newCalendar: CalendarDay[] = [];
		let newDay: CalendarDay;

		for (let i = 0; i < this.daysInCalendar; i++) {
			newDay = new CalendarDay({date: this.moveDate(DateUtils.formatDateToString(this.startDay), i)});
			newCalendar.push(newDay);
		}

		this.calendar = newCalendar;
	}

	sortTimeEntriesByDate(): void {
		this.setEmptyWeek();
		let newCalendar: CalendarDay[] = this.calendar;

		this.projectTimeEntries.forEach((timeEntry: TimeEntry) => {
			newCalendar.forEach((day: CalendarDay) => {
				if (moment(day.date).toDate().getDate() === moment(timeEntry.date).toDate().getDate()) {
					day.timeEntries.push(timeEntry);
				}
			});
		});
		this.calendar = newCalendar;
		this.calendarService.calendar = newCalendar;
	}

	ngOnDestroy() {
		this.timeEntriesSubscription.unsubscribe();
		this.subscriptionImpersonation.unsubscribe();
	}

	private moveDate(date: string, dif: number): string {
		let newDate = moment(date).toDate();
		return DateUtils.formatDateToString(newDate.setDate(newDate.getDate() + dif));
	}
}
