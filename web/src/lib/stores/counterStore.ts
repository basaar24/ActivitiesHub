import { makeAutoObservable } from 'mobx'

export default class CounterStore {
  title = 'Counter Store'
  count = 24

  constructor() {
    makeAutoObservable(this)
  }

  increment = (amount: number = 1) => {
    this.count += amount
  }

  decrement = (amount: number = 1) => {
    this.count -= amount
  }
}
