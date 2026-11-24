import { makeAutoObservable } from 'mobx'

export class UiStore {
  isLoading = false

  constructor() {
    makeAutoObservable(this)
  }

  setLoading(loading: boolean) {
    this.isLoading = loading
  }
}
