import { makeAutoObservable } from 'mobx'

export default class CounterStore {
  title = 'Counter Store'
  count = 24
  events: string[] = [`Initial count is ${this.count}`]

  constructor() {
    makeAutoObservable(this) // Automatically makes all properties observable and all methods actions
  }

  increment = (amount: number = 1) => {
    this.count += amount
    this.events.push(`Incremented by ${amount}, new count is ${this.count}`)
  }

  decrement = (amount: number = 1) => {
    this.count -= amount
    this.events.push(`Decremented by ${amount}, new count is ${this.count}`)
  }

  get eventCount() {
    // Computed property to get the number of events
    return this.events.length
  }
}
