/**
 * Created by daniel on 19/2/17.
 */

import {combineReducers} from 'redux'
import {reducer as globalReducer, GlobalState} from './reducers/global'

export default combineReducers({
    global: globalReducer
})

export interface State {
    global: GlobalState
}
