import { makeObservable, observable } from 'mobx'

export default class CounterStore {
  title = 'Counter Store'
  count = 0

  constructor() {
    makeObservable(this, {
      title: observable,
      count: observable,
    })
  }
}
