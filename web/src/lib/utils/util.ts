import { format, type DateArg } from 'date-fns'

export function formatDate(date: DateArg<Date>) {
  return format(date, 'eeee dd MMM yyyy h:mm a')
}
